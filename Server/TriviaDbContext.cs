using Microsoft.EntityFrameworkCore;

namespace Trivia_Game_Server;

public class TriviaDbContext : DbContext
{
    public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options){}

    public DbSet<TriviaQuestion> Questions { get; set; }
}