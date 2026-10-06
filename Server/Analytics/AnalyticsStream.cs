namespace Trivia_Game_Server.Analytics;

// How the consumer reads the event stream. The stream's key and size live in AnalyticsKeys.
public static class AnalyticsStream
{
    // The consumer group. Redis remembers, per group, which events it has handed
    // out and which have been acknowledged as done.
    public const string GroupName = "analytics";

    // Unique per process so multiple instances get their own pending list.
    public static readonly string ConsumerName =
        $"{Environment.MachineName}-{Environment.ProcessId}";
}
