using FantasyNBA.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FantasyNBA.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        public int TeamApiId { get; set; } // Balldontlie Team ID
        public DataSourceApi DataSourceApi { get; set; } // e.g., "Balldontlie"

        public string Conference { get; set; }
        public string Division { get; set; }
        public string City { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Abbreviation { get; set; }

        // Navigation property to players
        public ICollection<Player> Players { get; set; }
    }
}
