using Azure.Core;
using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using FantasyNBA.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FantasyNBA.ApiClients
{
    public class NbaApiClient : INbaApiClient
    {
        private readonly IExternalApiDataFetcher _fetcher;
        private readonly IApiParser _parser;
        private readonly ApiProviderSettings _settings;
        private readonly int _pageSize;

        public DataSourceApi DataSourceApi => DataSourceApi.NbaApi;

        public NbaApiClient(IExternalApiDataFetcher fetcher, IApiParser parser,IOptions<ApiProviderSettings> options, int pageSize)
        {
            _fetcher = fetcher;
            _parser = parser;
            _settings = options.Value;
            _pageSize = pageSize;
        }

        private Dictionary<string, string> ConstructApiHeaders()
        {
            var headers = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                headers.Add("x-rapidapi-key", _settings.ApiKey);
            }
            if (!string.IsNullOrWhiteSpace(_settings.ApiHost))
            {
                headers.Add("x-rapidapi-host", _settings.ApiHost);
            }

            return headers;
        }

        public async Task<List<Player>> FetchPlayersDataAsync()
        {
            var allPlayers = new List<Player>();
            var endpoint = $"{_settings.BaseUrl}players?limit={_pageSize}"; 

            var pages = await _fetcher.FetchDataAsync(endpoint, _settings.ApiKey, _parser.GetNextCursor);

            foreach (var page in pages)
            {
                var parsed = _parser.ParsePlayersResponse(page);
                allPlayers.AddRange(parsed);
            }

            return allPlayers;
        }

        public async Task<List<Team>> FetchTeamsDataAsync()
        {
            var endpoint = $"{_settings.BaseUrl.TrimEnd('/')}/teams";
            var headers = ConstructApiHeaders();

            var pages = await _fetcher.FetchDataAsync(endpoint, null, _ => null, headers);

            var allTeams = new List<Team>();

            foreach (var page in pages)
            {
                var parsed = _parser.ParseTeamsResponse(page);
                allTeams.AddRange(parsed);
            }

            return allTeams;
        }
    }
}
