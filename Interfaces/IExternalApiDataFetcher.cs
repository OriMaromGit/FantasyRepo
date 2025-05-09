using System.Text.Json.Nodes;

namespace FantasyNBA.Interfaces
{
    public interface IExternalApiDataFetcher
    {
        Task<List<dynamic>> FetchPlayersDataAsync(string baseUrl, string apiKey, Func<dynamic, string?> getNextCursor);
    }
}
