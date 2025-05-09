using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using FantasyNBA.Services;
using Microsoft.Extensions.Options;

namespace FantasyNBA.ApiClients
{
    public class BallDontLieApiClient : INbaApiClient
    {
        private readonly IExternalApiDataFetcher _fetcher;
        private readonly IApiParser<Player> _parser;
        private readonly ApiProviderSettings _settings;
        private readonly int _pageSize;

        public DataSourceApi DataSourceApi => DataSourceApi.BallDontLie;


        public BallDontLieApiClient(
            IExternalApiDataFetcher fetcher,
            IApiParser<Player> parser,
            IOptions<ApiProviderSettings> options,
            int pageSize)
        {
            _fetcher = fetcher;
            _parser = parser;
            _settings = options.Value;
            _pageSize = pageSize;
        }

        public async Task<List<Player>> FetchPlayersDataAsync()
        {
            var allPlayers = new List<Player>();
            var endpoint = $"{_settings.BaseUrl}players?per_page={_pageSize}";

            var pages = await _fetcher.FetchPlayersDataAsync(endpoint, _settings.ApiKey, _parser.GetNextCursor);

            foreach (var page in pages)
            {
                var parsed = _parser.ParsePlayersResponse(page);
                allPlayers.AddRange(parsed);
            }

            return allPlayers;
        }

        public async Task<List<Team>> FetchTeamsDataAsync()
        {
            var url = _settings.BaseUrl.TrimEnd('/') + "/teams";
            //var pages = await _fetcher.FetchTeamsDataAsync(_settings.BaseUrl, _settings.ApiKey,_ => "teams", _ => null);

            var allTeams = new List<Team>();
            /*foreach (var page in pages)
            {
                var parsed = _parser.ParseTeamsResponse(page);
                allTeams.AddRange(parsed);
            }
*/
            return allTeams;
        }
    }
}
