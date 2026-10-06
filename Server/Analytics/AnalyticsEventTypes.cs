namespace Trivia_Game_Server.Analytics;

// Event type names shared by the publisher and the consumer, so the two can
// never disagree. Past tense, because an event records something that already happened.
public static class AnalyticsEventTypes
{
    public const string PlayerLoggedIn = "player.logged_in";
    public const string MatchCreated = "match.created";
    public const string PlayerJoinedMatch = "player.joined";
    public const string PlayerLeftMatch = "player.left";
    public const string PlayerFinishedMatch = "player.finished";
}
