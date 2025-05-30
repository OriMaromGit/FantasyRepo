using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using FantasyNBA.Services;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Text;

namespace FantasyNBA.ApiClients
{
    public class BallDontLieApiClient : INbaApiClient
    {
        private readonly IExternalApiDataFetcher _fetcher;
        private readonly IApiParser _parser;
        private readonly ApiProviderSettings _settings;
        private readonly int _pageSize;
        private readonly IGenericLogger _loggerDb;
        private const int MAX_CONCURRENT_PLAYER_DATA_REQUESTS = 1; 

        public DataSourceApi DataSourceApi => DataSourceApi.BallDontLie;
      
        public BallDontLieApiClient(IExternalApiDataFetcher fetcher,IApiParser parser, IOptions<ApiProviderSettings> options, int pageSize, 
            IGenericLogger loggerDb)
        {
            _fetcher = fetcher;
            _parser = parser;
            _settings = options.Value;
            _pageSize = pageSize;
            _loggerDb = loggerDb;
        }


        private async Task LogMissingPlayerAsync(int playerId, string url, string reason)
        {
            await _loggerDb.LogAsync(new LogEntry
            {
                Category = "MissingPlayerData",
                Message = reason,
                Context = $"BallDontLie playerId: {playerId}, url: {url}"
            });
        }

        private async Task<HashSet<int>> FetchActivePlayerIdsAsync(IEnumerable<int> playerIds)
        {
            // Limit the number of concurrent HTTP requests using SemaphoreSlim
            using var semaphore = new SemaphoreSlim(MAX_CONCURRENT_PLAYER_DATA_REQUESTS);
            var tasks = new List<Task<int?>>();

            foreach (var playerId in playerIds)
            {
                tasks.Add(FetchPlayerIfActiveAsync(playerId, semaphore));
            }

            // Wait for all tasks to complete
            var results = await Task.WhenAll(tasks);

            // Filter out nulls and return only the IDs of active players
            return results.Where(id => id.HasValue).Select(id => id.Value).ToHashSet();
        }

        private async Task<int?> FetchPlayerIfActiveAsync(int playerId, SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();

            try
            {
                // Build the API URL for this specific player
                var url = $"{_settings.BaseUrl}players/{playerId}";

                // Send the HTTP request and get the response
                var playerData = await _fetcher.FetchDataAsync(url, _settings.ApiKey);

                // Check if the response contains valid player data and if the player is active
                dynamic data = playerData.FirstOrDefault()?.data;
                if (data != null && data.is_active == true)
                {
                    return playerId;
                }

                await LogMissingPlayerAsync(playerId, url, "No data or player inactive");
            }
            catch (Exception ex)
            {
                await LogMissingPlayerAsync(playerId, "", $"Exception occurred: {ex.Message}");
            }
            finally
            {
                // Always release the semaphore to avoid deadlocks
                semaphore.Release();
            }

            return null;
        }

        private async Task<List<Player>> FilterActivePlayersAsync(List<Player> allPlayers)
        {
            // Step 1: Extract BallDontLie Id's and map them to each Player
            var playerIdMap = allPlayers.Where(p => !string.IsNullOrWhiteSpace(p.ExternalApiDataJson))
                .Select(p =>
                {
                    var id = JsonConvert.DeserializeObject<Dictionary<string, int>>(p.ExternalApiDataJson)[DataSourceApi.BallDontLie.ToString()];
                    return new { Player = p, ExternalId = id };
                }).ToDictionary(x => x.ExternalId, x => x.Player);

            var allIds = playerIdMap.Keys;

            // Step 2: Fetch IDs of players who were active in requested seasons (based on stats data)
            var activeIds = await FetchActivePlayerIdsAsync(allIds);

            // Step 3: Filter Players to include only those whose BallDontLie ID is in the active list
            var activePlayers = allPlayers
                .Where(p =>
                {
                    var dict = JsonConvert.DeserializeObject<Dictionary<string, int>>(p.ExternalApiDataJson);
                    return dict != null && dict.TryGetValue("BallDontLie", out var id) && activeIds.Contains(id);
                }).ToList();

            return activePlayers;
        }

        public async Task<List<Player>> FetchPlayersDataAsync()
        {
            var allPlayers = new List<Player>();
            var endpoint = $"{_settings.BaseUrl}players?per_page={_pageSize}";

            var pages = await _fetcher.FetchDataAsync(endpoint, _settings.ApiKey, _parser.GetNextCursor);

            foreach (var page in pages)
            {
                //var players = _parser.ParsePlayersResponse(page); 
                //allPlayers.AddRange(players);
            }

            // Currnetly, since it is hard to tell the active players from this api - it will be skippd.
            var activePlayers = await FilterActivePlayersAsync(allPlayers);

            return allPlayers;
        }

        public async Task<List<Team>> GetTeamsAsync()
        {
            var endpoint = _settings.BaseUrl.TrimEnd('/') + "/teams";

            var pages = await _fetcher.FetchDataAsync(endpoint, _settings.ApiKey, _ => null);

            var allTeams = new List<Team>();
            foreach (var page in pages)
            {
                var parsed = _parser.ParseTeamsResponse(page);
                allTeams.AddRange(parsed);
            }

            return allTeams;
        }

        public Task<SyncResult<Player>> GetPlayersAsync(Dictionary<DataSourceApi, Dictionary<int, Team>> teamLookup, bool filterByIdOnly)
        {
            throw new NotImplementedException();
        }
    }
}
