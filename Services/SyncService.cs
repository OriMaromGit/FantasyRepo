using FantasyNBA.Data;
using FantasyNBA.DTOs;
using FantasyNBA.Enums;
using FantasyNBA.Helpers;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Utils;
using Microsoft.EntityFrameworkCore;

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
                var providerTeams = await client.GetTeamsAsync();
                _logger.LogInformation($"Fetched {providerTeams.Count} teams from {client.DataSourceApi}");

                fetchedTeams.AddRange(providerTeams);
            }

            return EntityMerger.MergeTeams(fetchedTeams);
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
            var groupedPlayers = new Dictionary<(DataSourceApi, int), List<Player>>();

            var teams = await _dbProvider.GetTeamsAsync();
            var teamLookup = ParserUtils.BuildDataSourceToTeamIdLookup(teams);

            foreach (var client in _apiClients)
            {
                if (_excludedApis.Contains(client.DataSourceApi))
                {
                    _logger.LogInformation("Skipping {Api} API for player sync.", client.DataSourceApi);
                    continue;
                }

                var syncResult = await client.GetPlayersAsync(teamLookup, filterByIdOnly: false);

                allNewPlayers.AddRange(syncResult.NewItems);
                allUpdatedPlayers.AddRange(syncResult.UpdatedItems);

                foreach (var kvp in syncResult.GroupedItems)
                {
                    groupedPlayers[kvp.Key] = kvp.Value;
                }
            }

            await _dbProvider.SavePlayersAsync(allNewPlayers, allUpdatedPlayers);

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
