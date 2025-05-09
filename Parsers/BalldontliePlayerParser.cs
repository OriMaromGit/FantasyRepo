using FantasyNBA.Enums;
using FantasyNBA.Models;
using Newtonsoft.Json.Linq;

public class BallDontLiePlayerParser : IApiParser<Player>
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
                TeamApiId = (int?)teamNode?.id ?? 0,
                City = (string?)teamNode?.city ?? "",
                Name = (string?)teamNode?.name ?? "",
                Abbreviation = (string?)teamNode?.abbreviation ?? "",
                FullName = (string?)teamNode?.full_name ?? "",
                Conference = (string?)teamNode?.conference ?? "",
                Division = (string?)teamNode?.division ?? ""
            };

            return new Player
            {
                DataSourceApi = DataSourceApi.BallDontLie,
                PlayerApiId = (int?)item?.id ?? 0,
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
                Team = team
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
}
