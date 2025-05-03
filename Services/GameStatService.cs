using FantasyNBA.Data;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Microsoft.EntityFrameworkCore;

namespace FantasyNBA.Services;

public class GameStatService : IGameStatService
{
    private readonly FantasyDbContext _context;

    public GameStatService(FantasyDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<GameStat>> GetStatsForPlayerAsync(int playerId)
    {
        return await _context.GameStats
            .Where(gs => gs.PlayerId == playerId)
            .Include(gs => gs.Player)
            .ToListAsync();
    }

    public async Task<GameStat> AddGameStatAsync(GameStat gameStat)
    {
        _context.GameStats.Add(gameStat);
        await _context.SaveChangesAsync();
        return gameStat;
    }
}
