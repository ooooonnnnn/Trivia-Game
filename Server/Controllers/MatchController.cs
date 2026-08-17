using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Trivia_Game_Server.Controllers;

[ApiController]
[Route("/[controller]")]
public class MatchController : ControllerBase
{
    private readonly TriviaDbContext _context;
    private readonly Lock _startMatchLock = new();
    
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
            
            await AddPlayerToOpenMatchOrCreate(player);
            return Ok(player);
        }
        player = registeredPlayers.First();
        
        //Check player not in match
        var playersInActiveMatch = await _context.PlayersInMatches
            .FromSqlRaw("select * from \"PlayersInMatches\" where \"MatchID\" in " +
                        "(select \"MatchID\" from \"Matches\" where " +
                        "(\"IsActive\" = true or \"IsCompleted\" = false)) " +
                        "and \"PlayerID\" = {0}", player.Id)
            .ToHashSetAsync();

        //player is in active match, can't login
        if (playersInActiveMatch.Count > 0)
        {
            return Conflict("Already in a match");
        }

        await AddPlayerToOpenMatchOrCreate(player);
        return Ok(player);
    }

    [HttpGet("is-active/{matchId}")]
    public async Task<IActionResult> IsMatchActive(int matchId)
    {
        Stopwatch sw = Stopwatch.StartNew();
        var foundMatch = await _context.Matches.FindAsync(matchId);
        if (foundMatch == null)
            return NotFound();
        
        Console.WriteLine($"Match {matchId} found in {sw.ElapsedMilliseconds}ms");
        return Ok(foundMatch.IsActive);
    }

    private async Task<TriviaMatch?> FindOpenMatch()
    {
        var foundMatches = await _context.Matches
            .FromSqlRaw("SELECT m.* " +
                        "FROM \"Matches\" m " +
                        "JOIN \"GameSettings\" gs ON true " +
                        "where m.\"IsCompleted\" = false " +
                        "and ( " +
                        "  select count(*) " +
                        "from \"PlayersInMatches\" " +
                        "where \"MatchID\" = m.id" +
                        ") < gs.\"MaxPlayers\" " +
                        "limit 1")
            .ToListAsync();

        if (foundMatches.Count == 0)
            return null;
        
        return foundMatches[0];
    }

    private async Task AddPlayerToOpenMatchOrCreate(TriviaPlayer player)
    {
        var openMatch = await FindOpenMatch();
        TriviaMatch matchToJoin;
        
        if (openMatch != null)
        {
            matchToJoin = openMatch;
        }
        else
        {
            matchToJoin = (await _context.Matches
                .FromSqlRaw("insert into \"Matches\" (\"IsActive\", \"IsCompleted\", \"Winner_PlayerID\") " +
                            "values (false, false, null) " +
                            "returning *")
                .ToListAsync()).First();
        }
        
        //put player in match
        await _context.PlayersInMatches
            .FromSqlRaw("insert into \"PlayersInMatches\" (\"MatchID\", \"PlayerID\") " +
                        "values ({0}, {1}) returning *", matchToJoin.Id, player.Id)
            .ToListAsync();
        
        //try to start the match
        lock (_startMatchLock)
        {
            StartMatchesWithEnoughPlayers();
        }
    }

    private async Task StartMatchesWithEnoughPlayers()
    {
        await _context.Database.ExecuteSqlRawAsync("update \"Matches\" set \"IsActive\" = true " +
                                                   "where \"IsActive\" = false " +
                                             "and id in (" +
                                             "  select \"MatchID\" from (" +
                                             "    select Count(*) c, \"MatchID\" from \"PlayersInMatches\" group by \"MatchID\"" +
                                             "  ) where c >= " +
                                             "    (select \"MinPlayers\" from \"GameSettings\" limit 1))");
    }
}