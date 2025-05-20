using FantasyNBA.Enums;
using FantasyNBA.Models;

namespace FantasyNBA.Interfaces
{
    public interface IDbProvider
    {
        Task<List<string>> GetAllTeamApiJsonsAsync();
        Task<List<Team>> GetAllTeamsAsync();
        Task AddLogEntryAsync(LogEntry entry);
        Task AddLogEntryAsync(string category, string message, string? context = null);
        Task<List<Player>> GetPlayersByExternalIdsAsync(IEnumerable<string> externalIds, DataSourceApi source);
    }
}
