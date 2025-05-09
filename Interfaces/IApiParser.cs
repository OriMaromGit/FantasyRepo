public interface IApiParser<T>
{
    IEnumerable<T> ParsePlayersResponse(dynamic response);
    string? GetNextCursor(dynamic response);
}
