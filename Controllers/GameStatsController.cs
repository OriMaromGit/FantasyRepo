using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Microsoft.AspNetCore.Mvc;

namespace FantasyNBA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameStatsController : ControllerBase
{
    private readonly IGameStatService _gameStatService;

    public GameStatsController(IGameStatService gameStatService)
    {
        _gameStatService = gameStatService;
    }

    [HttpGet("player/{playerId}")]
    public async Task<ActionResult<IEnumerable<GameStatDto>>> GetStatsForPlayer(int playerId)
    {
        var stats = await _gameStatService.GetStatsForPlayerAsync(playerId);

        var dtoList = stats.Select(s => new GameStatDto
        {
            GameDate = s.GameDate,
            Points = s.Points,
            Assists = s.Assists,
            Rebounds = s.Rebounds
        });

        return Ok(dtoList);
    }


    [HttpPost]
    public async Task<ActionResult<GameStat>> AddGameStat(GameStat stat)
    {
        var created = await _gameStatService.AddGameStatAsync(stat);
        return CreatedAtAction(nameof(GetStatsForPlayer), new { playerId = stat.PlayerId }, created);
    }


}
