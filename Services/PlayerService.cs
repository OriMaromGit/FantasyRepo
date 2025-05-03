using FantasyNBA.Data;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Microsoft.EntityFrameworkCore;

namespace FantasyNBA.Services;

public class PlayerService : IPlayerService
{
    private readonly FantasyDbContext _context;

    public PlayerService(FantasyDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Player>> GetAllPlayersAsync()
    {
        return await _context.Players.ToListAsync();
    }

    public async Task<Player?> GetPlayerByIdAsync(int id)
    {
        return await _context.Players.FindAsync(id);
    }

    public async Task<Player> AddPlayerAsync(Player player)
    {
        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        return player;
    }
}
