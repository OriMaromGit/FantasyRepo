namespace FantasyNBA.Models
{
    public class PlayerTeamHistory
    {
        public int Id { get; set; }

        public int PlayerId { get; set; }
        public Player Player { get; set; }

        public int TeamId { get; set; }
        public Team Team { get; set; }

        public int Season { get; set; }

        public DateTime? AcquiredAt { get; set; }  // Optional: when joined the team

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
