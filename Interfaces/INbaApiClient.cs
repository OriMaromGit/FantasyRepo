using FantasyNBA.Enums;
using FantasyNBA.Models;
using System.Threading.Tasks;

namespace FantasyNBA.Interfaces
{
    public interface INbaApiClient
    {
        DataSourceApi DataSourceApi { get; }

        Task<List<Player>> FetchPlayersDataAsync();
        Task<List<Team>> FetchTeamsDataAsync();
    }
}
