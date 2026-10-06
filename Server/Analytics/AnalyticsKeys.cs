using System.Globalization;

namespace Trivia_Game_Server.Analytics;

// Every Redis key the analytics layer uses: what each one holds and how long it
// is kept. All keys live under "trivia:" and run general to specific, so they
// group together when browsing and SCAN trivia:* finds all of them.
public static class AnalyticsKeys
{
    private const string Prefix = "trivia";

    // Stream: raw events published by the game. Keeps roughly the newest
    // MaxEventsKept events; older ones are trimmed away.
    public const string Events = Prefix + ":events";
    public const int MaxEventsKept = 10_000;

    // Hash: events that have already been counted, so a redelivery can't count
    // twice. Field = event id from trivia:events, value = that event's type.
    // Each field is removed an hour after it was added.
    public const string EventsCounted = Events + ":counted";
    public static readonly TimeSpan EventsCountedLifetime = TimeSpan.FromHours(1);

    // Hash: one day's counters (logins, matchesCreated, playersJoined,
    // playersLeft, playersFinished). Kept for 35 days after its last update.
    public static string DailyStats(DateOnly day) => $"{Prefix}:stats:daily:{Format(day)}";
    public static readonly TimeSpan DailyStatsLifetime = TimeSpan.FromDays(35);

    // Bitmap: players active on a day. Bit index = player id. Kept for 90 days
    // after its last update.
    public static string ActivePlayers(DateOnly day) => $"{Prefix}:players:active:{Format(day)}";
    public static readonly TimeSpan ActivePlayersLifetime = TimeSpan.FromDays(90);

    // Sorted set: players ranked by their total score across all matches.
    // Member = player name, score = total points (whole numbers). Kept forever.
    public const string LeaderboardTotalScore = Prefix + ":leaderboard:total_score";

    private static string Format(DateOnly day) =>
        day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
