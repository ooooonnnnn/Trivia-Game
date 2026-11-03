using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

[ApiController]
[Route("/[controller]")]
public class AnalyticsController : ControllerBase
{
    private const int MaxLeaderboardSize = 100;

    private readonly IConnectionMultiplexer? _redis;

    // redis is null when analytics is disabled.
    public AnalyticsController(IConnectionMultiplexer? redis = null)
    {
        _redis = redis;
    }

    // Day format is yyyy-MM-dd. Without a day, returns today (UTC).
    [HttpGet("daily/{day?}")]
    public async Task<IActionResult> GetDailyStats(DateOnly? day)
    {
        if (_redis is null)
            return AnalyticsDisabled();

        var db = _redis.GetDatabase();
        var statsDay = day ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var counters = await db.HashGetAllAsync(AnalyticsKeys.DailyStats(statsDay));
        var activePlayers = await db.StringBitCountAsync(AnalyticsKeys.ActivePlayers(statsDay));

        return Ok(new
        {
            day = statsDay,
            activePlayers,
            counters = counters.ToDictionary(c => c.Name.ToString(), c => (long)c.Value),
        });
    }

    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard(int top = 10)
    {
        if (_redis is null)
            return AnalyticsDisabled();

        top = Math.Clamp(top, 1, MaxLeaderboardSize);

        var entries = await _redis.GetDatabase().SortedSetRangeByRankWithScoresAsync(
            AnalyticsKeys.LeaderboardTotalScore, 0, top - 1, Order.Descending);

        return Ok(entries.Select((entry, index) => new
        {
            rank = index + 1,
            playerName = entry.Element.ToString(),
            totalScore = entry.Score,
        }));
    }

    private ObjectResult AnalyticsDisabled() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable, "Analytics is disabled");
}
