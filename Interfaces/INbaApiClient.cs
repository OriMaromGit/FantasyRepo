using FantasyNBA.Enums;
using FantasyNBA.Models;

namespace FantasyNBA.Interfaces
{
    public interface INbaApiClient
    {
        DataSourceApi DataSourceApi { get; }
        Task<SyncResult<Player>> GetPlayersAsync( Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup,bool filterByIdOnly); 
        Task<List<Team>> GetTeamsAsync();
    }
}
