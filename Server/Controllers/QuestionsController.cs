using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Trivia_Game_Server.Controllers;

[ApiController]
[Route("/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly TriviaDbContext _context;
    private readonly IMemoryCache _cache;
    private const int CACHE_SIZE = 100;
    private const int CACHE_LIFETIME_MINUTES = 10;

    public QuestionsController(TriviaDbContext context, IMemoryCache cache)
    {
        _context = context;
        _cache = cache;
    }
    
    [HttpGet("question")]
    public async Task<IActionResult> GetQuestion()
    {
        if (!_cache.TryGetValue(typeof(TriviaQuestion), out HashSet<TriviaQuestion> questions))
        {
            await PrepareQuestions();
            questions = _cache.Get<HashSet<TriviaQuestion>>(typeof(TriviaQuestion));
        }
        
        return Ok(questions.ElementAt(new Random().Next(questions.Count)));
    }

    [HttpGet("answers/{questionId}")]
    public async Task<IActionResult> GetAnswers(int questionId)
    {
        if (!_cache.TryGetValue(typeof(TriviaAnswer), out HashSet<TriviaAnswer> answers))
        {
            await PrepareQuestions();
            
            answers = _cache.Get<HashSet<TriviaAnswer>>(typeof(TriviaAnswer));
        }
        
        return Ok(answers
            .Where(answer => answer.QuestionId == questionId)
            .ToHashSet());
    }

    [HttpPost]
    public async Task<IActionResult> PrepareQuestions()
    {
        var questions = await _context.Questions
            .FromSqlRaw("SELECT * FROM \"Questions\" ORDER BY random() LIMIT {0}", CACHE_SIZE)
            .ToHashSetAsync();
        
        _cache.Set(typeof(TriviaQuestion), questions,
            TimeSpan.FromMinutes(CACHE_LIFETIME_MINUTES));

        var questionsIds = questions.Select(q => q.Id)
            .ToArray();
        
        var answers = await _context.Answers
            .FromSqlRaw("select * from \"Answers\" " +
                        "where \"QuestionID\" = any({0})", questionsIds)
            .ToHashSetAsync();
        
        _cache.Set(typeof(TriviaAnswer), answers,
            TimeSpan.FromMinutes(CACHE_LIFETIME_MINUTES));
        
        return Ok("Cache prepared");
    }
}