namespace FantasyNBA.Models.Config
{
    public class ApiProviderSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiHost { get; set; } = string.Empty;

        public int PageSize { get; set; } = 50;
    }
}