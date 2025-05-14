using FantasyNBA.Enums;
using FantasyNBA.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace FantasyNBA.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        public string? ExternalApiDataJson { get; set; }

        public string? Conference { get; set; }
        public string? Division { get; set; }
        public string? City { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Abbreviation { get; set; }
        public string? LogoUrl { get; set; } // URL to team logo
        public string? Nickname { get; set; } // Optional: "Celtics", "Lakers", etc.


        // Navigation property to players
        public ICollection<Player> Players { get; set; }
    
        public static (List<Team> newTeams, List<Team> updatedTeams) GetTeamsToSync(List<Team> incomingTeams, Dictionary<string, Team> existingTeamsByAbbreviation)
        {
            var newTeams = new List<Team>();
            var updatedTeams = new List<Team>();

            foreach (var team in incomingTeams)
            {

                if (existingTeamsByAbbreviation.TryGetValue(team.Abbreviation, out var existing))
                {
                    if (DbSyncHelper.TryMergeExternalApiIds(existing, team))
                    {
                        updatedTeams.Add(existing);
                    }
                }
                else
                {
                    newTeams.Add(team);
                }
            }

            return (newTeams, updatedTeams);
        }
    }
}
