using FantasyNBA.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

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

        public async Task<List<dynamic>> FetchDataAsync(string endpoint, string apiKey, Func<dynamic, string?>? getNextCursor = null, Dictionary<string, string>? headers = null)
        {
            var results = new List<dynamic>();
            string? cursor = null;

            do
            {
                var cleanEndpoint = Regex.Replace(endpoint, @"&cursor=\d+", "", RegexOptions.Compiled);

                if (!string.IsNullOrEmpty(cursor))
                {
                    cleanEndpoint += $"&cursor={cursor}";
                }

                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Get, cleanEndpoint);
                    if (!string.IsNullOrWhiteSpace(apiKey))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                    }
                    if (headers != null)
                    {
                        foreach (var header in headers)
                        {
                            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
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
                    cursor = getNextCursor != null ? getNextCursor(json) : null;

                    // Conservative delay to reduce chance of rate limiting
                    await Task.Delay(TimeSpan.FromSeconds(6));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch data from {Url}", cleanEndpoint);
                    break;
                }

            } while (!string.IsNullOrEmpty(cursor));

            return results;
        }

        public async Task<List<T>> FetchMultipleTeamSeasonPagesAsync<T>(IEnumerable<int> teamIds, IEnumerable<int> seasons, Func<int, int, string> endpointBuilder,
            string apiKey, Func<dynamic, string?>? getNextCursor = null, Dictionary<string, string>? headers = null, Func<dynamic, int, int, IEnumerable<T>> parsePage = null)
        {
            var results = new List<T>();

            var teamSeasonPairs = from season in seasons
                                  from teamId in teamIds
                                  orderby season descending
                                  select (teamId, season);

            foreach (var (teamId, season) in teamSeasonPairs)
            {
                var endpoint = endpointBuilder(teamId, season);
                _logger.LogInformation("Fetching players for Team ID {TeamId}, Season {Season}...", teamId, season);

                var pages = await FetchDataAsync(endpoint, apiKey, getNextCursor, headers);

                if (parsePage == null)
                {
                    _logger.LogWarning("Parse function is null. Skipping Team ID {TeamId}, Season {Season}.", teamId, season);
                    continue;
                }

                var parsedPlayers = pages.SelectMany<dynamic, T>(page => parsePage(page, teamId, season)).ToList();
                _logger.LogInformation("Fetched {Count} players for Team ID {TeamId}, Season {Season}.", parsedPlayers.Count, teamId, season);

                results.AddRange(parsedPlayers);
            }

            return results;
        }
    }
}
