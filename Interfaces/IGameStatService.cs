using FantasyNBA.Models;

namespace FantasyNBA.Interfaces;

public interface IGameStatService
{
    Task<IEnumerable<GameStat>> GetStatsForPlayerAsync(int playerId);
    Task<GameStat> AddGameStatAsync(GameStat gameStat);
}
