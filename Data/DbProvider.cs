using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Utils;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FantasyNBA.Data
{
    public class DbProvider : IDbProvider
    {
        private readonly FantasyDbContext _context;

        public DbProvider(FantasyDbContext context)
        {
            _context = context;
        }

        public async Task<List<Team>> GetTeamsAsync()
        {
            return await _context.Teams.ToListAsync();
        }

        public async Task<List<string>> GetAllTeamApiJsonsAsync()
        {
            return await _context.Teams
                .Where(t => !string.IsNullOrWhiteSpace(t.ExternalApiDataJson))
                .Select(t => t.ExternalApiDataJson)
                .ToListAsync();
        }

        public async Task AddLogEntryAsync(LogEntry entry)
        {
            _context.LogEntries.Add(entry);
            await _context.SaveChangesAsync();
        }

        public async Task AddLogEntryAsync(string category, string message, string? context = null)
        {
            var entry = new LogEntry
            {
                Category = category,
                Message = message,
                Context = context
            };

            await AddLogEntryAsync(entry);
        }

        public async Task<List<Player>> GetPlayersByExternalIdsAsync(IEnumerable<string> externalIds, DataSourceApi source)
        {
            if (externalIds == null || !externalIds.Any())
                return new List<Player>();

            var sourceKey = source.ToString();
            var candidates = await _context.Players
                .Where(p => p.ExternalApiDataJson.Contains($"\"{sourceKey}\""))
                .ToListAsync();

            var matchedPlayers = new List<Player>();

            foreach (var player in candidates)
            {
                var id = ParserUtils.ExtractApiId(player.ExternalApiDataJson, source);
                if (id != null && externalIds.Contains(id.Value.ToString()))
                {
                    matchedPlayers.Add(player);
                }
            }

            return matchedPlayers;
        }

        public async Task AddPlayersAsync(List<Player> newPlayers)
        {
            if (newPlayers.Any())
            {
                await _context.Players.AddRangeAsync(newPlayers);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdatePlayersAsync(List<Player> updatedPlayers)
        {
            if (updatedPlayers.Any())
            {
                _context.Players.UpdateRange(updatedPlayers);
                await _context.SaveChangesAsync();
            }
        }

        // Optional combo method
        public async Task SavePlayersAsync(List<Player> newPlayers, List<Player> updatedPlayers)
        {
            if (newPlayers.Any())
                await AddPlayersAsync(newPlayers);

            if (updatedPlayers.Any())
                await UpdatePlayersAsync(updatedPlayers);
        }

        public async Task AddPlayerTeamHistoryIfNewAsync(int playerId, int teamId, int season)
        {
            var exists = await _context.PlayerTeamHistories.AnyAsync(h =>
                h.PlayerId == playerId && h.TeamId == teamId && h.Season == season);

            if (!exists)
            {
                _context.PlayerTeamHistories.Add(new PlayerTeamHistory
                {
                    PlayerId = playerId,
                    TeamId = teamId,
                    Season = season,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Player>> GetPlayersByTeamAndSeasonAsync(int teamId, int season)
        {
            return await _context.PlayerTeamHistories
                .Where(h => h.TeamId == teamId && h.Season == season)
                .Select(h => h.Player)
                .Include(p => p.ActiveTeam)
                .ToListAsync();
        }
    }
}
