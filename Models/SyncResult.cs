using FantasyNBA.Enums;

namespace FantasyNBA.Models
{
    public class SyncResult<T>
    {
        public List<T> NewItems { get; set; } = new();
        public List<T> UpdatedItems { get; set; } = new();
        public Dictionary<(DataSourceApi Api, int ExternalId), List<T>> GroupedItems { get; set; } = new();
    }
}
