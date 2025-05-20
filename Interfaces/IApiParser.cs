using FantasyNBA.Models;

public interface IApiParser
{
    IEnumerable<Player> ParsePlayersResponse(dynamic response, int teamId, int season);

    IEnumerable<Team> ParseTeamsResponse(dynamic response);

    string? GetNextCursor(dynamic response);
}
