using FantasyNBA.Enums;
using FantasyNBA.Models;

namespace FantasyNBA.Interfaces
{
    public interface IDbProvider
    {
        Task<List<string>> GetAllTeamApiJsonsAsync();
        Task<List<Team>> GetTeamsAsync();
        Task AddLogEntryAsync(LogEntry entry);
        Task AddLogEntryAsync(string category, string message, string? context = null);
        Task<List<Player>> GetPlayersByExternalIdsAsync(IEnumerable<string> externalIds, DataSourceApi source);
        Task AddPlayersAsync(List<Player> newPlayers);
        Task UpdatePlayersAsync(List<Player> updatedPlayers);
        Task SavePlayersAsync(List<Player> newPlayers, List<Player> updatedPlayers);
        Task AddPlayerTeamHistoryIfNewAsync(int playerId, int teamId, int season);
        Task<List<Player>> GetPlayersByTeamAndSeasonAsync(int teamId, int season);
    }
}
