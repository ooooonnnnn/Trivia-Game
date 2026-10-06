namespace Trivia_Game_Server.Analytics;

public static class AnalyticsStream
{
    public const string GroupName = "analytics";

    // One name per process, so server instances don't share pending entries.
    public static readonly string ConsumerName =
        $"{Environment.MachineName}-{Environment.ProcessId}";
}
