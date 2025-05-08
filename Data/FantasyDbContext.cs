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
            // Store enum as string in DB
            modelBuilder.Entity<Player>()
                .Property(p => p.DataSourceApi)
                .HasConversion<string>();

            modelBuilder.Entity<Team>()
                .Property(t => t.DataSourceApi)
                .HasConversion<string>();

            // Optional: enforce ExternalId uniqueness per API
            modelBuilder.Entity<Player>()
                .HasIndex(p => new { p.PlayerApiId, p.DataSourceApi })
                .IsUnique();

            modelBuilder.Entity<Team>()
                .HasIndex(t => new { t.TeamApiId, t.DataSourceApi })
                .IsUnique();
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<GameStat> GameStats { get; set; }
        public DbSet<Team> Teams { get; set; }
    }
}
