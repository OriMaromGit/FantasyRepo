using FantasyNBA.Data;
using Microsoft.EntityFrameworkCore;

namespace FantasyNBA.Models
{
    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = string.Empty;  // e.g. "MissingPlayerData"
        public string Message { get; set; } = string.Empty;   // Error or issue description
        public string? Context { get; set; }                  // Optional: JSON or plain string context info

        public interface IGenericLogger
        {
            Task LogAsync(LogEntry entry);
        }

        public class GenericLogger : IGenericLogger
        {
            private readonly FantasyDbContext _context;

            public GenericLogger(FantasyDbContext context)
            {
                _context = context;
            }

            public async Task LogAsync(LogEntry entry)
            {
                _context.LogEntries.Add(entry);
                await _context.SaveChangesAsync();
            }
        }
    }
}
