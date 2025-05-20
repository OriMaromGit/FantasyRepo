using FantasyNBA.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FantasyNBA.Models
{
    public class Player
    {
        [Key]
        public int Id { get; set; }

        public DataSourceApi DataSourceApi { get; set; }  // e.g., "Balldontlie"
        public string? ExternalApiDataJson { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Position { get; set; }

        // Physical & background info
        public string Height { get; set; }
        public int? Weight { get; set; }
        public string JerseyNumber { get; set; }
        public string College { get; set; }
        public string Country { get; set; }

        // Draft info
        public int? DraftYear { get; set; }
        public int? DraftRound { get; set; }
        public int? DraftNumber { get; set; }
        public int? NbaStartYear { get; set; }
        public bool IsActive { get; set; }             // Needed to store active status from RapidAPI

        // Foreign key and navigation
        public int TeamId { get; set; }
        public Team Team { get; set; }

        // Historical stats (optional)
        public ICollection<GameStat> GameStats { get; set; }

        // Optional: Computed or external data
        [NotMapped]
        public double AveragePoints { get; set; }
    }
}
