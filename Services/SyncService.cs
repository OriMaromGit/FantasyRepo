using FantasyNBA.Data;
using FantasyNBA.DTOs;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        public async Task<PlayerSyncResult> SyncPlayersAsync()
        {
            var result = new PlayerSyncResult();
            var allPlayers = new List<Player>();

            foreach (var client in _apiClients)
            {
                _logger.LogInformation(@$"Starting to fetch players from {client.DataSourceApi} API");
                var players = await client.FetchPlayersDataAsync();
                _logger.LogInformation(@$"Fetched total of {players.Count} players from {client.DataSourceApi} API");

                foreach (var player in players)
                {
                    if (player.Team != null)
                    {
                        var existingTeam = await _context.Teams
                            .FirstOrDefaultAsync(t => t.TeamApiId == player.Team.TeamApiId);

                        if (existingTeam != null)
                        {
                            player.TeamId = existingTeam.Id;
                            player.Team = existingTeam;
                        }
                        else
                        {
                            // Optionally: skip or log teams that don't exist yet
                            continue;
                        }
                    }

                    allPlayers.Add(player);
                }
            }

            await _context.Players.AddRangeAsync(allPlayers);
            await _context.SaveChangesAsync();

            result.Added = allPlayers.Count;
            return result;
        }
    }
}
