using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using FantasyNBA.Models.Config;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FantasyNBA.Services
{
    public class NbaApiClient : INbaApiClient
    {
        private readonly HttpClient _http;
        private readonly BalldontlieApiSettings _settings;

        public NbaApiClient(HttpClient http, IOptions<BalldontlieApiSettings> settings)
        {
            _http = http;
            _settings = settings.Value;

            _http.BaseAddress = new Uri(_settings.BaseUrl);
            _http.DefaultRequestHeaders.Add("Authorization", _settings.ApiKey);
        }

        public async Task<IEnumerable<Player>> FetchPlayersAsync()
        {
            var parser = new BalldontliePlayerParser(_settings.PageSize);
            return await FetchAllPaginatedAsync(parser);
        }

        private async Task<List<T>> FetchAllPaginatedAsync<T>(IApiParser<T> parser)
        {
            var allItems = new List<T>();
            string? nextCursor = null;

            do
            {
                var endpoint = parser.BuildEndpoint(nextCursor);

                var response = await _http.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(json);

                if (jsonNode == null) break;

                var items = parser.ParsePlayersResponse(jsonNode);
                allItems.AddRange(items);

                nextCursor = parser.GetNextCursor(jsonNode);

            } while (!string.IsNullOrEmpty(nextCursor));

            return allItems;
        }
    }
}
