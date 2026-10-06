namespace Trivia_Game_Server.Analytics;

public static class AnalyticsEventTypes
{
    public const string PlayerLoggedIn = "player.logged_in";
    public const string MatchCreated = "match.created";
    public const string PlayerJoinedMatch = "player.joined";
    public const string PlayerLeftMatch = "player.left";
    public const string PlayerFinishedMatch = "player.finished";
}
