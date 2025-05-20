using FantasyNBA.Data;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;

namespace FantasyNBA.Utils
{
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
