using FantasyNBA.Models;
using Newtonsoft.Json;

namespace FantasyNBA.Utils
{
    public static class EntityMerger
    {
        /// <summary>
        /// Merges a list of potentially duplicate teams by comparing their full names
        /// using a fuzzy matching algorithm and consolidating their external API identifiers.
        /// </summary>
        public static List<Team> MergeTeams(List<Team> allTeams)
        {
            var mergedTeams = new List<Team>();
            var visited = new HashSet<Team>();

            foreach (var team in allTeams)
            {
                if (visited.Contains(team))
                    continue;

                var duplicates = allTeams
                    .Where(t => t != team &&
                                !visited.Contains(t) &&
                                NameComparer.GetCombinedScore(t.FullName, team.FullName) > 90)
                    .ToList();

                duplicates.Add(team);
                visited.UnionWith(duplicates);

                var merged = MergeTeamGroup(duplicates);
                mergedTeams.Add(merged);
            }

            return mergedTeams;
        }

        private static string Prefer(string current, string fallback)
        {
            return !string.IsNullOrWhiteSpace(current) ? current : fallback;
        }

        private static void FillMissingTeamFields(Team baseTeam, List<Team> allTeams)
        {
            foreach (var team in allTeams)
            {
                baseTeam.Conference = Prefer(baseTeam.Conference, team.Conference);
                baseTeam.Division = Prefer(baseTeam.Division, team.Division);
                baseTeam.City = Prefer(baseTeam.City, team.City);
                baseTeam.Name = Prefer(baseTeam.Name, team.Name);
                baseTeam.FullName = Prefer(baseTeam.FullName, team.FullName);
                baseTeam.Abbreviation = Prefer(baseTeam.Abbreviation, team.Abbreviation);
                baseTeam.LogoUrl = Prefer(baseTeam.LogoUrl, team.LogoUrl);
                baseTeam.Nickname = Prefer(baseTeam.Nickname, team.Nickname);
                // Players not merged here
            }
        }

        /// <summary>
        /// Combines a group of matching team entities into one,
        /// aggregating their external API IDs into a JSON structure.
        /// </summary>
        private static Team MergeTeamGroup(List<Team> group)
        {
            var baseTeam = group.First();
            var externalDataList = new List<Dictionary<string, object>>();

            foreach (var team in group)
            {
                if (string.IsNullOrEmpty(team.ExternalApiDataJson)) continue;

                var parsedJson = JsonConvert.DeserializeObject<Dictionary<string, object>>(team.ExternalApiDataJson);
                if (parsedJson != null)
                {
                    externalDataList.Add(parsedJson);
                }
            }

            var mergedExternalData = Utils.MergeDictionaries(externalDataList);

            FillMissingTeamFields(baseTeam, group);
            baseTeam.ExternalApiDataJson = JsonConvert.SerializeObject(mergedExternalData);

            return baseTeam;
        }
    }
}
