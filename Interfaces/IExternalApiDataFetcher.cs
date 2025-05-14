using System.Text.Json.Nodes;

namespace FantasyNBA.Interfaces
{
    public interface IExternalApiDataFetcher
    {
        Task<List<dynamic>> FetchDataAsync(string endpoint, string apiKey, Func<dynamic, string?> getNextCursor, Dictionary<string, string>? headers = null);
    }
}
