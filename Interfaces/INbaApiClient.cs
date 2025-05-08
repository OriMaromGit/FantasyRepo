using FantasyNBA.Models;

namespace FantasyNBA.Interfaces
{
    public interface INbaApiClient
    {
        Task<IEnumerable<Player>> FetchPlayersAsync();
    }
}
