using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Trivia_Game_Server.Controllers;

[ApiController]
[Route("/[controller]")]
public class PlayersController : ControllerBase
{
    private readonly TriviaDbContext _context;
    
    public PlayersController(TriviaDbContext context)
    {
        _context = context;
    }

    [HttpGet("player-names")]
    public async Task<IActionResult> GetPlayerNames([FromQuery] int[] playerIds)
    {
        var playerLookup = await _context.Players
            .Where(p => playerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        var playerNames = playerIds
            .Where(id => playerLookup.ContainsKey(id))
            .Select(id => playerLookup[id])
            .ToList();

        return Ok(playerNames);
    }
    
}
