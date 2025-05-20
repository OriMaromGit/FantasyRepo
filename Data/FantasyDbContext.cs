using FantasyNBA.Models;
using FantasyNBA.Enums;
using Microsoft.EntityFrameworkCore;

namespace FantasyNBA.Data
{
    public class FantasyDbContext : DbContext
    {
        public FantasyDbContext(DbContextOptions<FantasyDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>()
                .Property(p => p.DataSourceApi)
                .HasConversion<string>();
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<GameStat> GameStats { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
    }
}
