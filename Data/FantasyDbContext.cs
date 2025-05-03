using FantasyNBA.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FantasyNBA.Data;

public class FantasyDbContext : DbContext
{
    public FantasyDbContext(DbContextOptions<FantasyDbContext> options)
        : base(options) { }

    public DbSet<Player> Players { get; set; }
    public DbSet<GameStat> GameStats { get; set; }
}
