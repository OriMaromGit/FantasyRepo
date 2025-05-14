using FantasyNBA.DTOs;
using FantasyNBA.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FantasyNBA.Helpers
{
    public static class DbSyncHelper
    {
        public static async Task<SyncResult> SyncEntitiesAsync<T>(DbContext context, List<T> newEntities, List<T> updatedEntities, List<T>? deletedEntities = null) where T : class
        {
            int added = 0, updated = 0, deleted = 0;

            if (newEntities.Any())
            {
                await context.Set<T>().AddRangeAsync(newEntities);
                added = newEntities.Count;
            }

            if (updatedEntities.Any())
            {
                context.Set<T>().UpdateRange(updatedEntities);
                updated = updatedEntities.Count;
            }

            if (deletedEntities?.Any() == true)
            {
                context.Set<T>().RemoveRange(deletedEntities);
                deleted = deletedEntities.Count;
            }

            if (added > 0 || updated > 0 || deleted > 0)
                await context.SaveChangesAsync();

            return new SyncResult() { Added = added, Updated = updated, Deleted = deleted };
        }


        public static bool TryMergeExternalApiIds(Team existing, Team incoming)
        {
            try
            {
                var existingJson = JsonSerializer.Deserialize<Dictionary<string, int>>(existing.ExternalApiDataJson ?? "{}")!;
                var incomingJson = JsonSerializer.Deserialize<Dictionary<string, int>>(incoming.ExternalApiDataJson ?? "{}")!;

                bool changed = false;

                foreach (var kvp in incomingJson)
                {
                    if (!existingJson.ContainsKey(kvp.Key) || !Equals(existingJson[kvp.Key], kvp.Value))
                    {
                        existingJson[kvp.Key] = kvp.Value;
                        changed = true;
                    }
                }

                if (changed)
                    existing.ExternalApiDataJson = JsonSerializer.Serialize(existingJson);

                return changed;
            }
            catch
            {
                return false; // swallow error, or optionally log
            }
        }
    }
}
