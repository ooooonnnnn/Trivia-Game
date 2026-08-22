using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Trivia_Game_Server.Controllers;

[ApiController]
[Route("/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly TriviaDbContext _context;
    private const int CACHE_SIZE = 100;
    private const int CACHE_LIFETIME_MINUTES = 10;

    public QuestionsController(TriviaDbContext context, IMemoryCache cache)
    {
        _context = context;
    }
    
    [HttpGet("answers/{questionId}")]
    public async Task<IActionResult> GetAnswers(int questionId)
    {
        var answers = await _context.Answers.FromSqlRaw(
            "Select * from \"Answers\" Where \"QuestionID\" = {0}", questionId)
            .ToHashSetAsync();
        
        return Ok(answers);
    }
}