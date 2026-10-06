namespace Trivia_Game_Server.Analytics;

// Field names inside each event in trivia:events, shared by the publisher and
// the consumer so the two can never disagree.
public static class AnalyticsEventFields
{
    public const string Type = "type";
    public const string MatchId = "matchId";
    public const string PlayerId = "playerId";
    public const string PlayerName = "playerName";
    public const string Score = "score";
}
