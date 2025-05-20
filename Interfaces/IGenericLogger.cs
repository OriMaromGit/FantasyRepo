using FantasyNBA.Models;

namespace FantasyNBA.Interfaces
{
    public interface IGenericLogger
    {
        public Task LogAsync(LogEntry entry);
    }
}
