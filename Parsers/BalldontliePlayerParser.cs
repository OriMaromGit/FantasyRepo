using FantasyNBA.Enums;
using FantasyNBA.Models;

public class BalldontliePlayerParser : IApiParser<Player>
{
    private readonly int _pageSize;

    public BalldontliePlayerParser(int pageSize)
    {
        _pageSize = pageSize;
    }

    public string BuildEndpoint(string cursor = null)
    {
        var endpoint = $"players?per_page={_pageSize}";
        
        if (!string.IsNullOrEmpty(cursor))
        {
            endpoint += $"&cursor={cursor}";
        }
        return endpoint;
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

        foreach (var item in response.data.EnumerateArray())
        {
            players.Add(new Player
            {
                DataSourceApi = DataSourceApi.Balldontlie,
                PlayerApiId = item["id"]!.GetValue<int>(),
                FirstName = item["first_name"]?.GetValue<string>(),
                LastName = item["last_name"]?.GetValue<string>(),
                Position = item["position"]?.GetValue<string>(),
                Height = item["height"]?.GetValue<string>(),
                Weight = item["weight"]?.GetValue<int?>(),
                JerseyNumber = item["jersey_number"]?.GetValue<string>(),
                College = item["college"]?.GetValue<string>(),
                Country = item["country"]?.GetValue<string>(),

                DraftYear = item["draft_year"]?.GetValue<int?>(),
                DraftRound = item["draft_round"]?.GetValue<int?>(),
                DraftNumber = item["draft_number"]?.GetValue<int?>(),

                Team = new Team
                {
                    Id = item["team"]?["id"]!.GetValue<int>() ?? 0,
                    City = item["team"]?["city"]?.GetValue<string>(),
                    Name = item["team"]?["name"]?.GetValue<string>(),
                    Abbreviation = item["team"]?["abbreviation"]?.GetValue<string>(),
                    FullName = item["team"]?["full_name"]?.GetValue<string>(),
                    Conference = item["team"]?["conference"]?.GetValue<string>(),
                    Division = item["team"]?["division"]?.GetValue<string>()
                }
            });
        }

        return players;
    }
}
