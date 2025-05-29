using FantasyNBA.Data;
using FantasyNBA.DTOs;
using FantasyNBA.Enums;
using FantasyNBA.Helpers;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace FantasyNBA.Services
{
    public class SyncService
    {
        #region Fields & Constructor

        private readonly IEnumerable<INbaApiClient> _apiClients;
        private readonly FantasyDbContext _context;
        private readonly ILogger<SyncService> _logger;
        private readonly HashSet<DataSourceApi> _excludedApis = new() { DataSourceApi.BallDontLie };
        private readonly IDbProvider _dbProvider;
        private readonly IGenericLogger _dbGenericLogger;

        public SyncService(IEnumerable<INbaApiClient> apiClients, FantasyDbContext context, ILogger<SyncService> logger, IGenericLogger dbGenericLogger, IDbProvider provider)
        {
            _apiClients = apiClients;
            _context = context;
            _logger = logger;
            _dbProvider = provider;
            _dbGenericLogger = dbGenericLogger;
        }

        #endregion Fields & Constructor

        #region Private Methods

        /// <summary>
        /// Fetches team data from all API clients and merges results.
        /// </summary>
        private async Task<List<Team>> FetchAndMergeTeamsFromProvidersAsync()
        {
            var fetchedTeams = new List<Team>();

            foreach (var client in _apiClients)
            {
                _logger.LogInformation($"Fetching teams from {client.DataSourceApi}");
                var providerTeams = await client.FetchTeamsDataAsync();
                _logger.LogInformation($"Fetched {providerTeams.Count} teams from {client.DataSourceApi}");

                fetchedTeams.AddRange(providerTeams);
            }

            return EntityMerger.MergeTeams(fetchedTeams);
        }

        /// <summary>
        /// Extracts new and updated players based on API data and existing records.
        /// </summary>
        private async Task<(List<Player> NewPlayers, List<Player> UpdatedPlayers, Dictionary<(DataSourceApi, int), List<Player>>)> GetNewOrUpdatedPlayersAsync(INbaApiClient client, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, bool filterByIdOnly)
        {
            var source = client.DataSourceApi;
            var seenPlayers = new HashSet<(DataSourceApi, int, int)>();
            var groupedPlayers = new Dictionary<(DataSourceApi, int), List<Player>>();

            var players = await client.FetchPlayersDataAsync();
            _logger.LogInformation("Fetched {Count} players from {Source} API", players.Count, source);

            foreach (var player in players)
            {
                foreach (var (api, externalId) in ExtractPlayerExternalIds(player))
                {
                    var key = (api, externalId, player.Season);
                    if (!seenPlayers.Add(key)) continue;

                    var groupKey = (api, externalId);
                    if (!groupedPlayers.ContainsKey(groupKey))
                        groupedPlayers[groupKey] = new();

                    groupedPlayers[groupKey].Add(player);
                }
            }

            var externalIds = groupedPlayers.Keys.Where(k => k.Item1 == source).Select(k => k.Item2.ToString()).ToList();
            var existing = await _dbProvider.GetPlayersByExternalIdsAsync(externalIds, source);
            var existingMap = new Dictionary<int, Player>();

            foreach (var player in existing)
            {
                var id = await ParserUtils.ExtractApiIdAsync(player.ExternalApiDataJson, source, _dbGenericLogger);
                if (id.HasValue)
                    existingMap[id.Value] = player;
            }

            var (newPlayers, updatedPlayers) = FilterNewOrChangedPlayers(groupedPlayers, existingMap, teamLookup, source, filterByIdOnly);
            return (newPlayers, updatedPlayers, groupedPlayers);
        }

        /// <summary>
        /// Filters players into new and updated lists based on comparison to DB records.
        /// </summary>
        private (List<Player> NewPlayers, List<Player> UpdatedPlayers) FilterNewOrChangedPlayers(Dictionary<(DataSourceApi, int), List<Player>> groupedByApiAndId, Dictionary<int, Player> existing, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, DataSourceApi source, bool filterByIdOnly)
        {
            var newPlayers = new List<Player>();
            var updatedPlayers = new List<Player>();

            foreach (var ((api, externalId), versions) in groupedByApiAndId)
            {
                if (api != source) continue;

                var sorted = versions.OrderByDescending(v => v.Season).ToList();
                var mostRecent = sorted.First();
                var older = sorted.Skip(1);

                if (existing.TryGetValue(externalId, out var exist))
                {
                    if (filterByIdOnly)
                    {
                        _logger.LogInformation("Player {Id} exists (ID-only), skipping.", externalId);
                        continue;
                    }

                    if (exist.ApplyUpdatesIfChanged(mostRecent))
                    {
                        exist.CurrentTeamId = mostRecent.CurrentTeamId;
                        AssignTeamIfPossible(exist, teamLookup, source);
                        updatedPlayers.Add(exist);
                        _logger.LogInformation("Player {Id} changed → updated.", externalId);
                    }
                }
                else
                {
                    AssignTeamIfPossible(mostRecent, teamLookup, source);
                    newPlayers.Add(mostRecent);
                    _logger.LogInformation("Player {Id} is new → insert.", externalId);
                }
            }

            return (newPlayers, updatedPlayers);
        }

        /// <summary>
        /// Records player's team history based on season and changes.
        /// </summary>
        private async Task LogPlayerTeamHistoriesAsync(List<Player> newPlayers, List<Player> updatedPlayers, Dictionary<(DataSourceApi, int), List<Player>> grouped)
        {
            foreach (var player in newPlayers)
            {
                var externalId = await ParserUtils.ExtractApiIdAsync(player.ExternalApiDataJson, player.DataSourceApi, _dbGenericLogger);
                if (!externalId.HasValue) continue;

                var key = (player.DataSourceApi, externalId.Value);
                if (!grouped.TryGetValue(key, out var versions)) continue;

                var history = versions
                    .Where(v => v.Season != player.Season && v.CurrentTeamId.HasValue && v.CurrentTeamId != player.CurrentTeamId)
                    .ToList();

                foreach (var old in history)
                {
                    await _dbProvider.AddPlayerTeamHistoryIfNewAsync(player.Id, old.CurrentTeamId.Value, old.Season);
                    _logger.LogInformation("Player {Id}: added history season {Season} (team {TeamId})", player.Id, old.Season, old.CurrentTeamId);
                }
            }

            foreach (var player in updatedPlayers)
            {
                if (!player.CurrentTeamId.HasValue) continue;
                await _dbProvider.AddPlayerTeamHistoryIfNewAsync(player.Id, player.CurrentTeamId.Value, player.Season);
                _logger.LogInformation("Player {Id}: updated history season {Season} (team {TeamId})", player.Id, player.Season, player.CurrentTeamId);
            }
        }

        /// <summary>
        /// Matches external API team ID to internal team ID and assigns.
        /// </summary>
        private void AssignTeamIfPossible(Player player, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, DataSourceApi source)
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
                _logger.LogWarning("Player {First} {Last}: missing team {TeamId} in {Source}", player.FirstName, player.LastName, player.CurrentTeamId, source);
                player.CurrentTeamId = 0;
            }
        }

        /// <summary>
        /// Extracts API IDs from player's ExternalApiDataJson.
        /// </summary>
        private List<(DataSourceApi, int)> ExtractPlayerExternalIds(Player player)
        {
            var ids = new List<(DataSourceApi, int)>();
            try
            {
                var parsed = ParserUtils.ParseExternalDataFromJsons(player.ExternalApiDataJson);
                foreach (var (api, data) in parsed)
                {
                    if (data["id"] is JToken token && token.Type == JTokenType.Integer)
                        ids.Add((api, token.Value<int>()));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to extract IDs: {Message}", ex.Message);
            }

            return ids;
        }

        #endregion Private Methods

        #region Public Methods

        /// <summary>
        /// Synchronizes players across all external APIs, saves new/updated records, and logs team history.
        /// </summary>
        public async Task<SyncResult> SyncPlayersAsync()
        {
            var result = new SyncResult();
            var allNewPlayers = new List<Player>();
            var allUpdatedPlayers = new List<Player>();
            var groupedPlayersByApiAndExternalId = new Dictionary<(DataSourceApi, int), List<Player>>();

            var teams = await _dbProvider.GetTeamsAsync();
            var teamLookup = ParserUtils.BuildDataSourceToTeamIdLookup(teams);

            foreach (var client in _apiClients)
            {
                if (_excludedApis.Contains(client.DataSourceApi))
                {
                    _logger.LogInformation("Skipping {Api} API for player sync.", client.DataSourceApi);
                    continue;
                }

                var (newPlayers, updatedPlayers, groupedByExternalId) = await GetNewOrUpdatedPlayersAsync(client, teamLookup, false);

                allNewPlayers.AddRange(newPlayers);
                allUpdatedPlayers.AddRange(updatedPlayers);

                foreach (var kvp in groupedByExternalId)
                {
                    groupedPlayersByApiAndExternalId[kvp.Key] = kvp.Value;
                }
            }

            await _dbProvider.SavePlayersAsync(allNewPlayers, allUpdatedPlayers);
            await LogPlayerTeamHistoriesAsync(allNewPlayers, allUpdatedPlayers, groupedPlayersByApiAndExternalId);

            result.Added = allNewPlayers.Count;
            result.Updated = allUpdatedPlayers.Count;

            return result;
        }

        /// <summary>
        /// Synchronizes teams by fetching from all providers and merging.
        /// </summary>
        public async Task<SyncResult> SyncTeamsAsync()
        {
            var mergedTeams = await FetchAndMergeTeamsFromProvidersAsync();

            var activeTeamNames = TeamFilter.GetActiveTeamNames();
            var activeTeams = mergedTeams
                .Where(t => activeTeamNames.Contains(TeamFilter.CanonicalizeTeamName(t.FullName), StringComparer.OrdinalIgnoreCase))
                .Select(t =>
                {
                    t.FullName = TeamFilter.CanonicalizeTeamName(t.FullName);
                    t.City = TeamFilter.NormalizeCity(t.City);
                    return t;
                })
                .ToList();

            var existingTeams = await _context.Teams.ToListAsync();
            var existingMap = existingTeams.ToDictionary(t => t.Abbreviation, StringComparer.OrdinalIgnoreCase);

            var (newTeams, updatedTeams) = Team.GetTeamsToSync(activeTeams, existingMap);
            var result = await DbSyncHelper.SyncEntitiesAsync(_context, newTeams, updatedTeams);

            return result;
        }

        #endregion Public Methods
    }
}
