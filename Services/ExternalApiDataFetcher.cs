using FantasyNBA.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;

namespace FantasyNBA.Services
{
    public class ExternalApiDataFetcher : IExternalApiDataFetcher
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExternalApiDataFetcher> _logger;

        public ExternalApiDataFetcher(HttpClient http, ILogger<ExternalApiDataFetcher> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<List<dynamic>> FetchPlayersDataAsync(
            string endpoint,
            string apiKey,
            Func<dynamic, string?> getNextCursor)
        {
            var results = new List<dynamic>();
            string? cursor = null;

            do
            {
                if (!string.IsNullOrEmpty(cursor))
                {
                    endpoint += $"&cursor={cursor}";
                }

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    }

                    var response = await _http.SendAsync(request);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 2;
                        _logger.LogWarning("Rate limited. Retrying after {Seconds} seconds...", retryAfter);
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    dynamic json = JToken.Parse(content);

                    if (json == null) break;

                    results.Add(json);
                    cursor = getNextCursor(json);

                    // Conservative delay to reduce chance of rate limiting
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch data from {Url}", endpoint);
                    break;
                }

            } while (!string.IsNullOrEmpty(cursor));

            return results;
        }
    }
}
