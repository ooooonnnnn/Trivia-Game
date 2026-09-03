using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Trivia_Game_Server.Controllers;

[ApiController]
[Route("/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly TriviaDbContext _context;
    public SettingsController(TriviaDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _context.Settings.FromSqlRaw(
            "select * from \"GameSettings\" limit 1").FirstAsync();
        
        return Ok(settings);
    }
}