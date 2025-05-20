using System.Text.Json.Nodes;

namespace FantasyNBA.Interfaces
{
    public interface IExternalApiDataFetcher
    {
        Task<List<dynamic>> FetchDataAsync(string endpoint, string apiKey, Func<dynamic, string?> getNextCursor = null, Dictionary<string, string>? headers = null);

        Task<List<T>> FetchMultipleTeamSeasonPagesAsync<T>(IEnumerable<int> teamIds, IEnumerable<int> seasons, Func<int, int, string> endpointBuilder, string apiKey,
            Func<dynamic, string?>? getNextCursor = null, Dictionary<string, string>? headers = null, Func<dynamic, int, int, IEnumerable<T>>? parsePage = null);
    }
}
