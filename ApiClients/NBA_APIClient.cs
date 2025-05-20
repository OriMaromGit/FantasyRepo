using Azure.Core;
using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using FantasyNBA.Services;
using FantasyNBA.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text;

namespace FantasyNBA.ApiClients
{
    public class NbaApiClient : INbaApiClient
    {
        private readonly IExternalApiDataFetcher _fetcher;
        private readonly IDbProvider _dbProvider;
        private readonly IApiParser _parser;
        private readonly ApiProviderSettings _settings;
        private readonly int _pageSize;
        private readonly SyncSettings _syncSettings;
        private readonly IGenericLogger _logger;

        public DataSourceApi DataSourceApi => DataSourceApi.NbaApi;

        public NbaApiClient(IExternalApiDataFetcher fetcher, IDbProvider dbProvider, IApiParser parser, IOptions<ApiProviderSettings> options, int pageSize, IOptions<SyncSettings> syncSettings, IGenericLogger genericLogger)
        {
            _fetcher = fetcher;
            _dbProvider = dbProvider;
            _parser = parser;
            _settings = options.Value;
            _pageSize = pageSize;
            _syncSettings = syncSettings.Value;
            _logger = genericLogger;
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
            // Get NBA_API team IDs
            var teams = await _dbProvider.GetAllTeamsAsync();

            var idTasks = teams.Select(t =>
                ParserUtils.ExtractApiIdAsync(t.ExternalApiDataJson, DataSourceApi.NbaApi, _logger));

            var idResults = await Task.WhenAll(idTasks); 

            var teamIds = idResults.Where(id => id.HasValue && id == 1)
                .Select(id => id.Value).ToList();

            int currentSeason = DateTime.UtcNow.Year - 1;
            var seasons = Enumerable.Range(currentSeason - _syncSettings.ActiveSeasonsBack + 1, _syncSettings.ActiveSeasonsBack);

            var headers = ConstructApiHeaders();

            // Fetch players data for each team and season
            var players = await _fetcher.FetchMultipleTeamSeasonPagesAsync(teamIds, seasons, (teamId, season) => $"{_settings.BaseUrl}players?team={teamId}&season={season}",
                apiKey: "",  getNextCursor: _parser.GetNextCursor, headers: headers, parsePage: _parser.ParsePlayersResponse );

            return players;
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
