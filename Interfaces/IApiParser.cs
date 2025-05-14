using FantasyNBA.Models;

public interface IApiParser
{
    IEnumerable<Player> ParsePlayersResponse(dynamic response);

    IEnumerable<Team> ParseTeamsResponse(dynamic response);

    string? GetNextCursor(dynamic response);
}
