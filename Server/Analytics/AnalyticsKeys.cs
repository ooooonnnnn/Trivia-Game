using System.Globalization;

namespace Trivia_Game_Server.Analytics;

// Every Redis key the analytics layer uses, in one place. All keys live under
// "trivia:" and run general to specific, so they group together when browsing
// and SCAN trivia:* finds all of them.
public static class AnalyticsKeys
{
    private const string Prefix = "trivia";

    // Stream: raw events published by the game.
    public const string Events = Prefix + ":events";

    // Hash: events that have already been counted, so a redelivery can't count
    // twice. Field = event id from trivia:events. Each field expires on its own.
    public const string EventsCounted = Events + ":counted";

    // Hash: one day's counters (logins, matchesCreated, playersJoined, playersLeft,
    // playersFinished).
    public static string DailyStats(DateOnly day) => $"{Prefix}:stats:daily:{Format(day)}";

    // Bitmap: players active on a day. Bit index = player id.
    public static string ActivePlayers(DateOnly day) => $"{Prefix}:players:active:{Format(day)}";

    // Sorted set: players ranked by their total score across all matches.
    // Member = player name, score = total points (whole numbers).
    public const string LeaderboardTotalScore = Prefix + ":leaderboard:total_score";

    private static string Format(DateOnly day) =>
        day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
