using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Newtonsoft.Json;

namespace FantasyNBA.Parsers
{
    public class NBA_APIParser : IApiParser
    {
        public IEnumerable<Team> ParseTeamsResponse(dynamic response)
        {
            var teams = new List<Team>();

            foreach (var item in response.response)
            {
                try
                {
                    var standardLeague = item.leagues?.standard;

                    var team = new Team
                    {
                        City = item.city ?? string.Empty,
                        Name = item.nickname ?? string.Empty,
                        FullName = item.name ?? string.Empty,
                        Abbreviation = item.code ?? string.Empty,
                        Conference = standardLeague?.conference ?? string.Empty,
                        Division = standardLeague?.division ?? string.Empty,
                        LogoUrl = item.logo ?? string.Empty,
                        Nickname = item.nickname ?? string.Empty,
                        ExternalApiDataJson = JsonConvert.SerializeObject(new Dictionary<string, object>
                        {
                            {
                                DataSourceApi.NbaApi.ToString(), new Dictionary<string, object>
                                {
                                    { "id", item.id ?? 0 }
                                }
                            }
                        })
                    };

                    teams.Add(team);
                }
                catch (Exception ex)
                {
                    // Handle parsing errors if needed
                }
            }

            return teams;
        }

        public string? GetNextCursor(dynamic response)
        {
            // NBA API doesn't use cursor-based pagination
            return null;
        }

        public IEnumerable<Player> ParsePlayersResponse(dynamic response, int teamId, int season)
        {
            var results = new List<Player>();

            if (response == null || response.response == null)
                return results;

            foreach (var item in response.response)
            {
                bool isActive = (bool?)item?.leagues?.standard?.active ?? false;
                if (!isActive)
                    continue;

                try
                {
                    var player = new Player
                    {
                        FirstName = (string?)item.firstname ?? string.Empty,
                        LastName = (string?)item.lastname ?? string.Empty,
                        Position = (string?)item.leagues?.standard?.pos ?? string.Empty,
                        Height = (string?)item.height?.meters ?? string.Empty,
                        Weight = double.TryParse((string?)item.weight?.kilograms, out var w) ? (int?)Math.Round(w) : null,
                        JerseyNumber = item.leagues?.standard?.jersey?.ToString() ?? string.Empty,
                        College = (string?)item.college ?? string.Empty,
                        Country = (string?)item.birth?.country ?? string.Empty,
                        DraftYear = null,
                        DraftRound = null,
                        DraftNumber = null,
                        CurrentTeamId = teamId,
                        IsActive = true,
                        Season = season,
                        NbaStartYear = (int?)item.nba?.start,
                        DataSourceApi = DataSourceApi.NbaApi,
                        ExternalApiDataJson = JsonConvert.SerializeObject(new Dictionary<string, object>
                        {
                            {
                                DataSourceApi.NbaApi.ToString(), // This becomes "NbaApi"
                                new Dictionary<string, int>
                                {
                                    { "id", (int)item.id }
                                }
                            }
                        })
                    };

                    results.Add(player);
                }
                catch
                {
                    // log or handle bad item if needed
                }
            }

            return results;
        }

    }
}
