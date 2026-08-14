using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Trivia_Game_Server.Controllers;

[ApiController]
[Route("/[controller]")]
public class MatchController : ControllerBase
{
    private readonly TriviaDbContext _context;
    
    public MatchController(TriviaDbContext context)
    {
        _context = context;
    }

    [HttpPost("login/{playerName}")]
    public async Task<IActionResult> Login(string playerName)
    {
        //TODO: validate player name again sql injection
        
        //Check player in Players table
        var registeredPlayers = await _context.Players
            .FromSqlRaw("select * from \"Players\" where (\"Name\") = {0}", playerName)
            .ToListAsync();

        TriviaPlayer player;
        if (registeredPlayers.Count == 0)
        {
            var newPlayer = await _context.Players
                .FromSqlRaw("insert into \"Players\" (\"Name\") values ({0}) returning *",
                    playerName)
                .ToListAsync();

            player = newPlayer[0];
            
            //TODO: add player to match
            return Ok(player);
        }
        player = registeredPlayers.First();
        
        //Check player not in match
        var playersInActiveMatch = await _context.PlayersInMatches
            .FromSqlRaw("select * from \"PlayersInMatches\" where \"MatchID\" in " +
                        "(select \"MatchID\" from \"Matches\" where \"IsActive\" = true) " +
                        "and \"PlayerID\" = {0}", player.Id)
            .ToHashSetAsync();

        //player is in active match, can't login
        if (playersInActiveMatch.Count > 0)
        {
            return Conflict("Already in a match");
        }
        
        return Ok(player);
    }

    // [HttpGet("match/can-start/{matchId}")]
    // public async Task<IActionResult> CanStartMatch(int matchId)
    // {
    //     var foundMatch = await _context.
    // }
}