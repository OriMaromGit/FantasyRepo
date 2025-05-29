using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Newtonsoft.Json.Linq;

namespace FantasyNBA.Utils
{
    public static class ParserUtils
    {
        private static async Task<int?> LogAndReturnNullAsync(IGenericLogger logger, string message, string catagory, string? context = null)
        {
            await logger.LogAsync(new LogEntry
            {
                Category = catagory,
                Message = message,
                Context = context
            });

            return null;
        }

        public static int? ExtractApiId(string json, DataSourceApi api)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var sections = ParseExternalDataFromJsons(json);

                if (!sections.TryGetValue(api, out var section))
                {
                    return null;
                }

                var idToken = section["id"];
                if (idToken != null && idToken.Type == JTokenType.Integer)
                {
                    return idToken.Value<int>();
                }
            }
            catch
            {
                // Silent fail
            }

            return null;
        }

        public static Dictionary<DataSourceApi, JObject> ParseExternalDataFromJsons(string json)
        {
            var result = new Dictionary<DataSourceApi, JObject>();

            if (string.IsNullOrWhiteSpace(json))
                return result;

            try
            {
                var jObject = JObject.Parse(json);

                foreach (var property in jObject.Properties())
                {
                    if (Enum.TryParse<DataSourceApi>(property.Name, ignoreCase: true, out var dataSource))
                    {
                        if (property.Value is JObject sectionObject)
                        {
                            result[dataSource] = sectionObject;
                        }
                    }
                }
            }
            catch
            {
                // Optional: log or skip
            }

            return result;
        }

        public static async Task<int?> ExtractApiIdAsync(string json, DataSourceApi api, IGenericLogger logger)
        {
            const string category = "Json Extraction";

            if (string.IsNullOrWhiteSpace(json))
            {
                return await LogAndReturnNullAsync(logger, $"Empty or null JSON received for API: {api}", category);
            }

            try
            {
                var id = ExtractApiId(json, api);

                if (id.HasValue)
                {
                    return id;
                }
                else
                {
                    return await LogAndReturnNullAsync(logger, $"Missing or invalid 'id' field for {api}", category, json);
                }
            }
            catch (Exception ex)
            {
                return await LogAndReturnNullAsync(logger, $"Exception occurred while parsing JSON for {api}", category, ex.ToString());
            }
        }

        /// <summary>
        /// Builds a lookup dictionary for fast access to DB teams by external API id.
        /// Structure: [DataSourceApi] → [external team id] → Team
        /// </summary>
        public static Dictionary<DataSourceApi, Dictionary<int, Team>> BuildDataSourceToTeamIdLookup(List<Team> teams)
        {
            var teamLookup = new Dictionary<DataSourceApi, Dictionary<int, Team>>();

            foreach (var team in teams)
            {
                if (string.IsNullOrWhiteSpace(team.ExternalApiDataJson))
                    continue;

                var sections = ParseExternalDataFromJsons(team.ExternalApiDataJson);

                foreach (var (api, section) in sections)
                {
                    var idToken = section["id"];
                    if (idToken?.Type != JTokenType.Integer)
                        continue;

                    var externalId = idToken.Value<int>();

                    if (!teamLookup.TryGetValue(api, out var apiMap))
                    {
                        apiMap = new Dictionary<int, Team>();
                        teamLookup[api] = apiMap;
                    }

                    apiMap[externalId] = team;
                }
            }

            return teamLookup;
        }
    }
}

