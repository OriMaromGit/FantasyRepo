using FantasyNBA.Interfaces;
using FantasyNBA.Models;
using Microsoft.AspNetCore.Mvc;

namespace FantasyNBA.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayersController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Player>>> GetAllPlayers()
    {
        var players = await _playerService.GetAllPlayersAsync();
        return Ok(players);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Player>> GetPlayerById(int id)
    {
        var player = await _playerService.GetPlayerByIdAsync(id);
        if (player == null) return NotFound();
        return Ok(player);
    }

    [HttpPost]
    public async Task<ActionResult<Player>> AddPlayer(Player player)
    {
        var created = await _playerService.AddPlayerAsync(player);
        return CreatedAtAction(nameof(GetPlayerById), new { id = created.Id }, created);
    }
}
