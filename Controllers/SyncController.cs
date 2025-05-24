using FantasyNBA.ApiClients;
using FantasyNBA.Services;
using Microsoft.AspNetCore.Mvc;

namespace FantasyNBA.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly SyncService _syncService;

        public SyncController(SyncService syncService)
        {
            _syncService = syncService;
        }

        [HttpPost("players")]
        public async Task<IActionResult> SyncPlayers()
        {
            var result = await _syncService.SyncPlayersAsync();
            return Ok($"{result.Added} new players added; {result.Updated} players updated; {result.Deleted} players deleted.");
        }

        [HttpPost("teams")]
        public async Task<IActionResult> SyncTeam()
        {
            var result = await _syncService.SyncTeamsAsync();
            return Ok($"{result} new teams added.");
        }
    }
}
