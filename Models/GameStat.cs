using System.ComponentModel.DataAnnotations.Schema;

namespace FantasyNBA.Models;

public class GameStat
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public DateTime GameDate { get; set; }

    public int Points { get; set; }

    public int Assists { get; set; }

    public int Rebounds { get; set; }

    [ForeignKey("PlayerId")]
    public Player? Player { get; set; }
}
