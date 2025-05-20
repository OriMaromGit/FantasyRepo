using FantasyNBA.Data;
using FantasyNBA.DTOs;
using FantasyNBA.Enums;
using FantasyNBA.Helpers;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace FantasyNBA.Services
{
    public class SyncService
    {
        private readonly IEnumerable<INbaApiClient> _apiClients;
        private readonly FantasyDbContext _context;
        private readonly ILogger<SyncService> _logger;
        private readonly HashSet<DataSourceApi> _excludedApis = new() { DataSourceApi.BallDontLie };
        private readonly IDbProvider _dbProvider;
        private readonly IGenericLogger _dbGenericLogger;

        public enum SyncActionType { Insert, Update }

        public record PlayerSyncAction(Player Player, SyncActionType Action);

        public SyncService(IEnumerable<INbaApiClient> apiClients, FantasyDbContext context, ILogger<SyncService> logger, IGenericLogger dbGenericLogger, IDbProvider provider)
        {
            _apiClients = apiClients;
            _context = context;
            _logger = logger;
            _dbProvider = provider;
            _dbGenericLogger = dbGenericLogger;
        }

        #region Private Methods

        private bool PlayerEquals(Player a, Player b)
        {
            return JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b);
        }

        private async Task<bool> ShouldSkipPlayerAsync(Player player, HashSet<int> seenExternalIds)
        {
            if (player.TeamId == 0)
                return true;

            var externalId = await ParserUtils.ExtractApiIdAsync(player.ExternalApiDataJson, DataSourceApi.NbaApi, _dbGenericLogger);
            if (externalId == null)
            {
                return true;
            }

            return !seenExternalIds.Add(externalId.Value); // if already seen, skip
        }

        private void AssignTeamIfPossible(Player player, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, DataSourceApi source)
        {
            if (teamLookup.TryGetValue(source, out var teamMap) &&
                teamMap.TryGetValue(player.TeamId, out var matchedTeam))
            {
                player.Team = matchedTeam;
            }
            else
            {
                _logger.LogWarning("Could not find matching team for player {First} {Last} (TeamId: {TeamId}) from {Source}",
                    player.FirstName, player.LastName, player.TeamId, source);

                player.TeamId = 0;
            }
        }

        private List<(DataSourceApi Source, int ExternalId)> ExtractAllApiIds(Player player)
        {
            var ids = new List<(DataSourceApi, int)>();

            try
            {
                var sections = ParserUtils.ParseExternalDataFromJsons(player.ExternalApiDataJson);
                foreach (var entry in sections)
                {
                    if (entry.Value["id"] is JToken idToken && idToken.Type == JTokenType.Integer)
                    {
                        ids.Add((entry.Key, idToken.Value<int>()));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Json Extraction", $"Failed to extract API IDs from player JSON: {ex.Message}");
            }

            return ids;
        }

        private List<Player> FilterNewOrChangedPlayers(Dictionary<int, Player> newPlayersById, Dictionary<int, Player> existingPlayersById, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup,
            DataSourceApi source, bool filterByIdOnly)
        {
            var playersToSync = new List<Player>();

            foreach (var (externalId, newPlayer) in newPlayersById)
            {
                var isExisting = existingPlayersById.TryGetValue(externalId, out var existing);

                if (filterByIdOnly && isExisting)
                {
                    _logger.LogInformation("Player {Id} exists in DB (filtered by ID-only), skipping.", externalId);
                    continue;
                }

                if (isExisting)
                {
                    var wasUpdated = existing.ApplyUpdatesIfChanged(newPlayer);

                    if (wasUpdated)
                    {
                        AssignTeamIfPossible(existing, teamLookup, source);
                        playersToSync.Add(existing); // updated version
                        _logger.LogInformation("Player {Id} from {Source} changed → marked for update.", externalId, source);
                    }
                    else
                    {
                        _logger.LogInformation("Player {Id} from {Source} unchanged, skipping.", externalId, source);
                    }
                }
                else
                {
                    // New player → assign team and insert
                    AssignTeamIfPossible(newPlayer, teamLookup, source);
                    playersToSync.Add(newPlayer);
                    _logger.LogInformation("Player {Id} from {Source} is new → marked for insert.", externalId, source);
                }
            }

            return playersToSync;
        }



        private async Task<List<Player>> GetNewOrUpdatedPlayersAsync( INbaApiClient client, Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, bool filterByIdOnly = true)
        {
            var source = client.DataSourceApi;
            var filteredPlayers = new List<Player>();
            var seenPlayers = new HashSet<(DataSourceApi Api, int ExternalId)>();
            var relevantPlayersById = new Dictionary<int, Player>();

            var players = await client.FetchPlayersDataAsync();
            _logger.LogInformation("Fetched total of {Count} players from {Source} API", players.Count, source);

            // STEP 1: Process each player from the API
            foreach (var player in players)
            {
                var ids = ExtractAllApiIds(player); // sync version

                foreach (var (api, externalId) in ids)
                {
                    // Skip if we've already seen this (Api, Id) combo
                    var dedupKey = (api, externalId);
                    if (!seenPlayers.Add(dedupKey))

                    // Only keep players from the current source
                    if (api == source)
                    {
                        relevantPlayersById[externalId] = player;
                    }
                }
            }

            // Step 2: Fetch existing players by external IDs from DB
            var externalIds = relevantPlayersById.Keys.Select(id => id.ToString()).ToList();
            var existingPlayers = await _dbProvider.GetPlayersByExternalIdsAsync(externalIds, source);

            var existingMap = new Dictionary<int, Player>();
            foreach (var existing in existingPlayers)
            {
                // Map external Ids to a dictionary
                var existingId = await ParserUtils.ExtractApiIdAsync(existing.ExternalApiDataJson, source, _dbGenericLogger);
                if (existingId != null)
                {
                    existingMap[existingId.Value] = existing;
                }
            }

            // Step 3: Compare and keep only new or updated players
            var playersToSync = FilterNewOrChangedPlayers(relevantPlayersById, existingMap, teamLookup, source, filterByIdOnly);
            _logger.LogInformation("Filtered {Count} new/updated players from {Source} API", playersToSync.Count, source);

            return playersToSync;
        }


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

            var mergedTeams = EntityMerger.MergeTeams(fetchedTeams);

            return mergedTeams;
        }

        #endregion Private Methods

        #region Public Methods

        public async Task<SyncResult> SyncPlayersAsync()
        {
            var result = new SyncResult();
            var allPlayers = new List<Player>();

            var teams = await _dbProvider.GetAllTeamsAsync();
            var teamLookup = ParserUtils.BuildTeamLookup(teams);

            foreach (var client in _apiClients)
            {
                if (_excludedApis.Contains(client.DataSourceApi))
                {
                    _logger.LogInformation("Skipping {Api} API for player sync.", client.DataSourceApi);
                    continue;
                }

                var players = await GetNewOrUpdatedPlayersAsync(client, teamLookup);
                allPlayers.AddRange(players);
            }

            var duplicatesInBatch = allPlayers
                .GroupBy(p => new { p.Id, p.DataSourceApi })
                .Where(g => g.Count() > 1)
                .ToList();

            if (allPlayers.Any())
            {
                await _context.Players.AddRangeAsync(allPlayers);
                await _context.SaveChangesAsync();
            }

            result.Added = allPlayers.Count;
            return result;
        }

        public async Task<SyncResult> SyncTeamsAsync()
        {
            // Step 1: Fetch and merge teams from all providers
            var mergedTeams = await FetchAndMergeTeamsFromProvidersAsync();

            // Step 2: Filter only active teams
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

            // Step 3: Load existing teams from DB and index them by abbreviation
            var existingTeams = await _context.Teams.ToListAsync();
            var existingMap = existingTeams.ToDictionary(t => t.Abbreviation, StringComparer.OrdinalIgnoreCase);

            // Step 4: Determine new vs updated teams
            var (newTeams, updatedTeams) = Team.GetTeamsToSync(activeTeams, existingMap);

            // Step 5: Sync to DB using reusable helper
            var result = await DbSyncHelper.SyncEntitiesAsync(_context, newTeams, updatedTeams);

            return result;
        } 
        
        #endregion Public Methods
    }
}
