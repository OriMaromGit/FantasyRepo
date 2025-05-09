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
            var added = await _syncService.SyncPlayersAsync();
            return Ok($"{added} new players added.");
        }
    }
}
