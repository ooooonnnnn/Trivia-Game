using System.Globalization;

namespace Trivia_Game_Server.Analytics;

// All Redis keys used by the analytics layer.
public static class AnalyticsKeys
{
    private const string Prefix = "trivia";

    // Stream of game events, trimmed to roughly the newest MaxEventsKept.
    public const string Events = Prefix + ":events";
    public const int MaxEventsKept = 10_000;

    // Hash of event ids that were already counted (value = event type).
    public const string EventsCounted = Events + ":counted";
    public static readonly TimeSpan EventsCountedLifetime = TimeSpan.FromHours(1);

    // Hash of counters for one day.
    public static string DailyStats(DateOnly day) => $"{Prefix}:stats:daily:{Format(day)}";
    public static readonly TimeSpan DailyStatsLifetime = TimeSpan.FromDays(35);

    // Bitmap of players active on a day, indexed by player id.
    public static string ActivePlayers(DateOnly day) => $"{Prefix}:players:active:{Format(day)}";
    public static readonly TimeSpan ActivePlayersLifetime = TimeSpan.FromDays(90);

    // Sorted set of player names by total score. Never expires.
    public const string LeaderboardTotalScore = Prefix + ":leaderboard:total_score";

    private static string Format(DateOnly day) =>
        day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
