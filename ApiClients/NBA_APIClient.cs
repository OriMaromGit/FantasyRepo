using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using Microsoft.Extensions.Options;

namespace FantasyNBA.ApiClients
{
    public class NbaApiClient : INbaApiClient
    {
        private readonly ApiProviderSettings _settings;
        private readonly SyncSettings _syncSettings;
        private readonly IExternalApiDataFetcher _fetcher;
        private readonly IDbProvider _dbProvider;
        private readonly IApiParser _parser;
        private readonly IGenericLogger _genericLogger;
        private readonly ILogger<INbaApiClient> _logger;
        private readonly int _pageSize;

        public DataSourceApi DataSourceApi => DataSourceApi.NbaApi;

        public NbaApiClient(IExternalApiDataFetcher fetcher, IDbProvider dbProvider, IApiParser parser, IOptions<ApiProviderSettings> options, int pageSize, IOptions<SyncSettings> syncSettings,
            IGenericLogger genericLogger, ILogger<INbaApiClient> logger)
        {
            _fetcher = fetcher;
            _dbProvider = dbProvider;
            _parser = parser;
            _settings = options.Value;
            _pageSize = pageSize;
            _syncSettings = syncSettings.Value;
            _genericLogger = genericLogger;
            _logger = logger;
        }

        #region Private Methods

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

        private async Task<List<Player>> FetchPlayersDataAsync(Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup)
        {
            var currentSeason = DateTime.UtcNow.Year - 1;
            var seasons = Enumerable.Range(currentSeason - _syncSettings.ActiveSeasonsBack + 1, _syncSettings.ActiveSeasonsBack);
            var headers = ConstructApiHeaders();

            if (!teamLookup.TryGetValue(DataSourceApi.NbaApi, out var teamMap))
            {
                _logger.LogWarning("No team mapping found for {Api}", DataSourceApi.NbaApi);
                return new();
            }

            var teamIds = teamMap.Select(kvp => kvp.Key).ToList();

            var players = await _fetcher.FetchMultipleTeamSeasonPagesAsync(teamIds, seasons, (teamId, season) => $"{_settings.BaseUrl}players?team={teamId}&season={season}", apiKey: string.Empty,
                getNextCursor: _parser.GetNextCursor, headers: headers, parsePage: _parser.ParsePlayersResponse);

            return players;
        }

        #endregion Private Methods

        #region Public Methods

        /// <summary>
        ///  Talk to the external API and parse response into List of Players
        /// </summary>
        /// <returns></returns>
        public async Task<SyncResult<Player>> GetPlayersAsync(Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, bool filterByIdOnly)
        {
            return await NbaApiClientUtils.GetNewOrUpdatedPlayersAsync(fetchPlayers: () => FetchPlayersDataAsync(teamLookup), source: DataSourceApi,
                teamLookup: teamLookup, filterByIdOnly: filterByIdOnly, dbProvider: _dbProvider, dbLogger: _genericLogger, logger: _logger);
        }

        public async Task<List<Team>> GetTeamsAsync()
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

        #endregion Public Methods
    }
    }
