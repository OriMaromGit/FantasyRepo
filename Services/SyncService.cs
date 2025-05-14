using FantasyNBA.Data;
using FantasyNBA.DTOs;
using FantasyNBA.Helpers;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Utils;
using Microsoft.EntityFrameworkCore;

namespace FantasyNBA.Services
{
    public class SyncService
    {
        private readonly IEnumerable<INbaApiClient> _apiClients;
        private readonly FantasyDbContext _context;
        private readonly ILogger<SyncService> _logger;

        public SyncService(IEnumerable<INbaApiClient> apiClients, FantasyDbContext context, ILogger<SyncService> logger)
        {
            _apiClients = apiClients;
            _context = context;
            _logger = logger;
        }


        public async Task<SyncResult> SyncPlayersAsync()
        {
            var result = new SyncResult();
            var allPlayers = new List<Player>();

            foreach (var client in _apiClients)
            {
                _logger.LogInformation(@$"Starting to fetch players from {client.DataSourceApi} API");
                var players = await client.FetchPlayersDataAsync();
                _logger.LogInformation(@$"Fetched total of {players.Count} players from {client.DataSourceApi} API");
            }

            await _context.Players.AddRangeAsync(allPlayers);
            await _context.SaveChangesAsync();

            result.Added = allPlayers.Count;
            return result;
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
    }
}
