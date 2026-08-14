using Microsoft.EntityFrameworkCore;

namespace Trivia_Game_Server;

public class TriviaDbContext : DbContext
{
    public TriviaDbContext(DbContextOptions<TriviaDbContext> options) : base(options){}

    public DbSet<TriviaQuestion> Questions { get; set; }
    public DbSet<TriviaAnswer> Answers { get; set; }
    public DbSet<TriviaPlayer> Players { get; set; }
    public DbSet<Trivia_PlayersInMatches> PlayersInMatches { get; set; }
    public DbSet<TriviaMatch> Matches { get; set; }
}