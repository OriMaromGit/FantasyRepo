using FantasyNBA.Models;

public interface IFantasyDataProvider
{
    Task<IEnumerable<Player>> FetchPlayersAsync();
}