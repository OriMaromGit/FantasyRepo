using FantasyNBA.Enums;
using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Newtonsoft.Json;

namespace FantasyNBA.Parsers
{
    public class NBA_APIParser : IApiParser
    {
        public IEnumerable<Team> ParsePlayersResponse(dynamic response)
        {
            // This parser does not handle players
            return Enumerable.Empty<Team>();
        }

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
                        ExternalApiDataJson = JsonConvert.SerializeObject(new
                        {
                            NBA_API_Id = item.id ?? 0
                        })
                    };

                    teams.Add(team);
                }
                catch
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

        IEnumerable<Player> IApiParser.ParsePlayersResponse(dynamic response)
        {
            throw new NotImplementedException();
        }
    }
}
