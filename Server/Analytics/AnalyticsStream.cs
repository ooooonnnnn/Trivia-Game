namespace Trivia_Game_Server.Analytics;

public static class AnalyticsStream
{
    public const string Key = "events";
    public const string GroupName = "analytics";
    // Unique per process so multiple instances get their own pending list.
    public static readonly string ConsumerName =
        $"{Environment.MachineName}-{Environment.ProcessId}";
    public const int MaxLength = 10000;
}
