namespace FantasyNBA.Models;

public class Player
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Team { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public double AveragePoints { get; set; } = 0.0;

    public ICollection<GameStat>? GameStats { get; set; }
}
