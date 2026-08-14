using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Trivia_Game_Server.Controllers;

[ApiController]
[Route("/[controller]")]
public class QuestionsController : ControllerBase
{
    private readonly TriviaDbContext _context;
    public QuestionsController(TriviaDbContext context) => _context = context;
    
    private const int QUESTION_CACHE_SIZE = 100;
    private HashSet<TriviaQuestion> questionCache = [];
    
    // [HttpGet("raw")]
    public async Task<IActionResult> GetQuestionsRaw()
    {
        var items = await _context.Questions
            .FromSqlRaw("SELECT * FROM \"Questions\"")
            .ToListAsync();
        
        return Ok(items);
    }
    
    // [HttpGet]
    public async Task<IActionResult> GetQuestions()
    {
        var items = await _context.Questions
            .FromSqlRaw("SELECT * FROM \"Questions\"")
            .Select(question => question.QuestionText)
            .ToListAsync();
        
        return Ok(items);
    }

    [HttpGet("question")]
    public async Task<IActionResult> GetQuestion()
    {
        var item = questionCache.ElementAt(new Random().Next(questionCache.Count));
        
        return Ok(item);
    }

    // [HttpGet("answers {questionId}")]
    // public async Task<IActionResult> GetAnswers(int questionId)
    // {
    //     
    // }

    [HttpPost]
    public async Task<IActionResult> PrepareQuestions()
    {
        var items = await _context.Questions
            .FromSqlRaw("SELECT * FROM \"Questions\" ORDER BY random() LIMIT {0}", QUESTION_CACHE_SIZE)
            .ToHashSetAsync();
        
        questionCache = items;
        
        return Ok("Question cache prepared");
    }
}