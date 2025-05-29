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
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Player>()
                .HasOne(p => p.ActiveTeam)
                .WithMany() // or .WithMany(t => t.Players) if you want reverse nav
                .HasForeignKey(p => p.CurrentTeamId)
                .OnDelete(DeleteBehavior.NoAction); // Prevent cascading path

            modelBuilder.Entity<PlayerTeamHistory>()
                .HasOne(h => h.Team)
                .WithMany() // or .WithMany(t => t.TeamHistories)
                .HasForeignKey(h => h.TeamId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<PlayerTeamHistory>()
                .HasOne(h => h.Player)
                .WithMany(p => p.TeamHistory)
                .HasForeignKey(h => h.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // ✅ Add uniqueness constraint to prevent duplicates per source+season
            modelBuilder.Entity<Player>()
                .HasIndex(p => new { p.ExternalApiDataJson, p.Season, p.DataSourceApi })
                .IsUnique()
                .HasDatabaseName("IX_Player_UniquePerSourceSeason");
        }



        public DbSet<Player> Players { get; set; }
        public DbSet<GameStat> GameStats { get; set; }
        public DbSet<Team> Teams { get; set; }
        public DbSet<LogEntry> LogEntries { get; set; }
        public DbSet<PlayerTeamHistory> PlayerTeamHistories { get; set; }

    }
}
