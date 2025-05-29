using FantasyNBA.Enums;
using FantasyNBA.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class BallDontLiePlayerParser : IApiParser
{
    
    public BallDontLiePlayerParser()
    {
    }

    private static Player? ParsePlayer(dynamic item)
    {
        try
        {
            dynamic teamNode = item.team;

            var team = new Team
            {
                City = (string?)teamNode?.city ?? "",
                Name = (string?)teamNode?.name ?? "",
                Abbreviation = (string?)teamNode?.abbreviation ?? "",
                FullName = (string?)teamNode?.full_name ?? "",
                Conference = (string?)teamNode?.conference ?? "",
                Division = (string?)teamNode?.division ?? "",
                ExternalApiDataJson = JsonConvert.SerializeObject(new
                {
                    BallDontLie = (int?)teamNode?.id ?? 0
                })
            };

            var externalIds = new Dictionary<string, int>
            {
                [DataSourceApi.BallDontLie.ToString()] = (int?)item?.id ?? 0
            };

            return new Player
            {
                DataSourceApi = DataSourceApi.BallDontLie,
                FirstName = (string?)item?.first_name ?? "",
                LastName = (string?)item?.last_name ?? "",
                Position = (string?)item?.position ?? "",
                Height = (string?)item?.height ?? "",
                Weight = (int?)item?.weight,
                JerseyNumber = (string?)item?.jersey_number ?? "",
                College = (string?)item?.college ?? "",
                Country = (string?)item?.country ?? "",
                DraftYear = (int?)item?.draft_year,
                DraftRound = (int?)item?.draft_round,
                DraftNumber = (int?)item?.draft_number,
                ActiveTeam = team,
                ExternalApiDataJson = JsonConvert.SerializeObject(externalIds)
            };
        }
        catch
        {
            return null;
        }
    }

    public string? GetNextCursor(dynamic response)
    {
        try
        {
            return response?.meta?.next_cursor?.ToString();
        }
        catch
        {
            return null;
        }
    }

    public IEnumerable<Player> ParsePlayersResponse(dynamic response)
    {
        var players = new List<Player>();

        foreach (var item in response.data)
        {
            var player = ParsePlayer(item);
            if (player != null)
            {
                players.Add(player);
            }
        }

        return players;
    }

    public IEnumerable<Player> ParsePlayersResponse(dynamic response, int teamId, int season)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Team> ParseTeamsResponse(dynamic response)
    {
        var teams = new List<Team>();

        foreach (var item in response.data)
        {
            try
            {
                var team = new Team
                {
                    City = (string?)item?.city ?? "",
                    Name = (string?)item?.name ?? "",
                    FullName = (string?)item?.full_name ?? "",
                    Abbreviation = (string?)item?.abbreviation ?? "",
                    Conference = (string?)item?.conference ?? "",
                    Division = (string?)item?.division ?? "",
                    Nickname = null, // Not provided in BallDontLie response
                    LogoUrl = null, // Not provided in BallDontLie response
                    ExternalApiDataJson = JsonConvert.SerializeObject(new Dictionary<string, object>
                    {
                        {
                            DataSourceApi.BallDontLie.ToString(), new Dictionary<string, int>
                            {
                                { "id", (int?)item?.id ?? 0 }
                            }
                        }
                    })
                };

                teams.Add(team);
            }
            catch
            {
                // Optional: log parse error for this item
            }
        }

        return teams;
    }

}
