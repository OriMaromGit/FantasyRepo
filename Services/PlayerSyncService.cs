using FantasyNBA.Data;
using FantasyNBA.DTOs;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Microsoft.EntityFrameworkCore;

namespace FantasyNBA.Services
{
    public class PlayerSyncService
    {
        private readonly INbaApiClient _apiClient;
        private readonly FantasyDbContext _context;

        public PlayerSyncService(INbaApiClient apiClient, FantasyDbContext context)
        {
            _apiClient = apiClient;
            _context = context;
        }

        private bool PlayerChanged(Player existing, Player external)
        {
            return existing.Position != external.Position
                || existing.Height != external.Height
                || existing.Weight != external.Weight
                || existing.Team.TeamApiId != external.Team.TeamApiId;
        }

        private void UpdatePlayer(Player target, Player source)
        {
            target.Position = source.Position;
            target.Height = source.Height;
            target.Weight = source.Weight;

            var existingTeam = _context.Teams
                .FirstOrDefault(t => t.TeamApiId == source.Team.TeamApiId);

            if (existingTeam != null)
            {
                target.TeamId = existingTeam.Id;
                target.Team = existingTeam;
            }
        }

        public async Task<PlayerSyncResult> SyncPlayersAsync()
        {
            var externalPlayers = (await _apiClient.FetchPlayersAsync()).ToList();
            var result = new PlayerSyncResult();

            var externalKeys = externalPlayers
                .Select(p => (p.PlayerApiId, p.DataSourceApi))
                .ToHashSet();

            var existingPlayers = await _context.Players
                .Include(p => p.Team)
                .ToListAsync();

            // Delete players not in external source
            var toDelete = existingPlayers
                .Where(p => !externalKeys.Contains((p.PlayerApiId, p.DataSourceApi)))
                .ToList();

            _context.Players.RemoveRange(toDelete);
            result.Deleted = toDelete.Count;

            foreach (var external in externalPlayers)
            {
                var existing = existingPlayers
                    .FirstOrDefault(p => p.PlayerApiId == external.PlayerApiId && p.DataSourceApi == external.DataSourceApi);

                if (existing == null)
                {
                    // Link to existing team if available
                    var existingTeam = await _context.Teams
                        .FirstOrDefaultAsync(t => t.TeamApiId == external.Team.TeamApiId);

                    if (existingTeam != null)
                    {
                        external.TeamId = existingTeam.Id;
                        external.Team = existingTeam;
                    }

                    _context.Players.Add(external);
                    result.Added++;
                }
                else if (PlayerChanged(existing, external))
                {
                    UpdatePlayer(existing, external);
                    result.Updated++;
                }
            }

            await _context.SaveChangesAsync();
            return result;
        }

    }
}
