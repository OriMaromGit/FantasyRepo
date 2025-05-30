using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Utils;
using Newtonsoft.Json.Linq;

public static class NbaApiClientUtils
{
    #region Private Methods

    /// <summary>
    /// Groups player versions by their API and external ID while removing duplicates by (api, id, season).
    /// </summary>
    private static Dictionary<(DataSourceApi, int), List<Player>> GroupPlayersByExternalId( List<Player> players, ILogger logger)
    {
        var seen = new HashSet<(DataSourceApi, int, int)>();
        var grouped = new Dictionary<(DataSourceApi, int), List<Player>>();

        foreach (var player in players)
        {
            foreach (var (api, externalId) in ExtractPlayerExternalIds(player, logger))
            {
                var key = (api, externalId, player.Season);
                if (!seen.Add(key)) { continue; }

                var groupKey = (api, externalId);
                if (!grouped.ContainsKey(groupKey))
                {
                    grouped[groupKey] = new List<Player>();
                }

                grouped[groupKey].Add(player);
            }
        }

        return grouped;
    }

    /// <summary>
    /// Builds a dictionary of existing DB players by external ID for a specific API source.
    /// </summary>
    private static async Task<Dictionary<int, Player>> BuildExistingPlayersMapAsync(Dictionary<(DataSourceApi, int), List<Player>> grouped, DataSourceApi source, IDbProvider dbProvider, IGenericLogger dbLogger)
    {
        var externalIds = grouped.Keys.Where(k => k.Item1 == source).Select(k => k.Item2.ToString()).ToList();

        var existing = await dbProvider.GetPlayersByExternalIdsAsync(externalIds, source);
        var existingMap = new Dictionary<int, Player>();

        foreach (var player in existing)
        {
            var id = await ParserUtils.ExtractApiIdAsync(player.ExternalApiDataJson, source, dbLogger);
            if (id.HasValue)
            {
                existingMap[id.Value] = player;
            }
        }

        return existingMap;
    }

    /// <summary>
    /// Adds team history records to each player based on seasonal changes in team.
    /// </summary>
    private static void AppendTeamHistory( Dictionary<(DataSourceApi, int), List<Player>> grouped, List<Player> trackedPlayers, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup,
        DataSourceApi source, ILogger logger)
    {
        foreach (var ((api, externalId), versions) in grouped)
        {
            if (api != source)
            {
                continue;
            }

            // Sort by season descending to identify most recent
            var sorted = versions.OrderByDescending(v => v.Season).ToList();
            var mostRecent = sorted.First();

            foreach (var version in sorted.Skip(1))
            {
                // Only consider entries where team is different from most recent
                if (version.CurrentTeamId.HasValue && version.CurrentTeamId != mostRecent.CurrentTeamId)
                {
                    if (!teamLookup.TryGetValue(api, out var teamMap) || 
                        !teamMap.TryGetValue(version.CurrentTeamId.Value, out var matchingTeam))
                    {
                        continue;
                    }

                    // Try find the player from those that were tracked
                    var player = trackedPlayers.FirstOrDefault(p =>
                        ExtractPlayerExternalIds(p, logger).Any(e => e.Item1 == api && e.Item2 == externalId));

                    if (player != null)
                    {
                        player.TeamHistory.Add(new PlayerTeamHistory
                        {
                            TeamId = matchingTeam.Id,
                            Season = version.Season
                        });
                    }
                }
            }
        }
    }

    /// <summary>
    /// Extracts external API identifiers from a player's external data JSON.
    /// </summary>
    private static List<(DataSourceApi, int)> ExtractPlayerExternalIds(Player player, ILogger logger)
    {
        var ids = new List<(DataSourceApi, int)>();
        try
        {
            var parsed = ParserUtils.ParseExternalDataFromJsons(player.ExternalApiDataJson);
            foreach (var (api, data) in parsed)
            {
                if (data["id"] is JToken token && token.Type == JTokenType.Integer)
                {
                    ids.Add((api, token.Value<int>()));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to extract IDs: {Message}", ex.Message);
        }

        return ids;
    }

    /// <summary>
    /// Filters the grouped players into new and updated players based on DB comparison.
    /// </summary>
    private static (List<Player> NewPlayers, List<Player> UpdatedPlayers) FilterNewOrChangedPlayers( Dictionary<(DataSourceApi, int), List<Player>> groupedByApiAndId, Dictionary<int, Player> existing,
        Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, DataSourceApi source, bool filterByIdOnly, ILogger logger)
    {
        var newPlayers = new List<Player>();
        var updatedPlayers = new List<Player>();

        foreach (var ((api, externalId), versions) in groupedByApiAndId)
        {
            if (api != source) 
            { 
                continue; 
            }

            var mostRecent = versions.OrderByDescending(v => v.Season).First();

            if (existing.TryGetValue(externalId, out var exist))
            {
                if (filterByIdOnly)
                {
                    logger.LogInformation("Player {Id} exists (ID-only), skipping.", externalId);
                    continue;
                }

                if (exist.ApplyUpdatesIfChanged(mostRecent))
                {
                    exist.CurrentTeamId = mostRecent.CurrentTeamId;
                    AssignTeamIfPossible(exist, teamLookup, source, logger);
                    updatedPlayers.Add(exist);
                    logger.LogInformation("Player {Id} changed → updated.", externalId);
                }
            }
            else
            {
                AssignTeamIfPossible(mostRecent, teamLookup, source, logger);
                newPlayers.Add(mostRecent);
                logger.LogInformation("Player {Id} is new → insert.", externalId);
            }
        }

        return (newPlayers, updatedPlayers);
    }

    /// <summary>
    /// Assigns a team reference to the player from the team lookup, if available.
    /// </summary>
    private static void AssignTeamIfPossible(Player player, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, DataSourceApi source, ILogger logger)
    {
        if (player.CurrentTeamId.HasValue &&
            teamLookup.TryGetValue(source, out var map) &&
            map.TryGetValue(player.CurrentTeamId.Value, out var team))
        {
            player.ActiveTeam = team;
            player.CurrentTeamId = team.Id;
        }
        else
        {
            logger.LogWarning("Player {First} {Last}: missing team {TeamId} in {Source}", player.FirstName, player.LastName, player.CurrentTeamId, source);
            player.CurrentTeamId = 0;
        }
    }

    #endregion Private Methods

    #region Public Methods

    /// <summary>
    /// Main entry point to fetch, group, compare and return new or updated players from a given data source.
    /// </summary>
    public static async Task<SyncResult<Player>> GetNewOrUpdatedPlayersAsync(Func<Task<List<Player>>> fetchPlayers, DataSourceApi source, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup,
        bool filterByIdOnly, IDbProvider dbProvider, IGenericLogger dbLogger, ILogger logger)
    {
        // Fetch players from the API
        var players = await fetchPlayers();
        logger.LogInformation("Fetched {Count} players from {Source} API", players.Count, source);

        // Group players by (API, externalId) to aggregate seasonal data
        var grouped = GroupPlayersByExternalId(players, logger);

        // Fetch players from DB and build a map of existing externalId → Player
        var existingMap = await BuildExistingPlayersMapAsync(grouped, source, dbProvider, dbLogger);

        // Compare new players to DB and separate into new or updated
        var (newPlayers, updatedPlayers) = FilterNewOrChangedPlayers(grouped, existingMap, teamLookup, source, filterByIdOnly, logger);

        // Attach historical team data to each player (older seasons, previous teams)
        AppendTeamHistory(grouped, newPlayers.Concat(updatedPlayers).ToList(), teamLookup, source, logger);

        return new SyncResult<Player>
        {
            NewItems = newPlayers,
            UpdatedItems = updatedPlayers,
            GroupedItems = grouped
        };
    }

    #endregion Public Methods
}
