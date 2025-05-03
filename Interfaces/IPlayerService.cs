using FantasyNBA.Models;

namespace FantasyNBA.Interfaces;

public interface IPlayerService
{
    Task<IEnumerable<Player>> GetAllPlayersAsync();
    Task<Player?> GetPlayerByIdAsync(int id);
    Task<Player> AddPlayerAsync(Player player);
}
