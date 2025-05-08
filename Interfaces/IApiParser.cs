public interface IApiParser<T>
{
    string BuildEndpoint(string cursor = null);
    IEnumerable<T> ParsePlayersResponse(dynamic response);
    string? GetNextCursor(dynamic response);
}
