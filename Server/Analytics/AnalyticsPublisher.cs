using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

public class AnalyticsPublisher
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnalyticsPublisher> _logger;

    public AnalyticsPublisher(IConnectionMultiplexer redis, ILogger<AnalyticsPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public Task PlayerLoggedInAsync(int playerId) =>
        PublishAsync(AnalyticsEventTypes.PlayerLoggedIn, new NameValueEntry("playerId", playerId));

    public Task MatchCreatedAsync(int matchId) =>
        PublishAsync(AnalyticsEventTypes.MatchCreated, new NameValueEntry("matchId", matchId));

    public Task PlayerJoinedMatchAsync(int matchId, int playerId) =>
        PublishAsync(AnalyticsEventTypes.PlayerJoinedMatch,
            new NameValueEntry("matchId", matchId),
            new NameValueEntry("playerId", playerId));

    public Task PlayerLeftMatchAsync(int matchId, int playerId) =>
        PublishAsync(AnalyticsEventTypes.PlayerLeftMatch,
            new NameValueEntry("matchId", matchId),
            new NameValueEntry("playerId", playerId));

    // Analytics must never slow down or break the game, so this never throws
    // and never waits for Redis to reply.
    private Task PublishAsync(string type, params NameValueEntry[] fields)
    {
        try
        {
            var entries = new NameValueEntry[fields.Length + 1];
            entries[0] = new NameValueEntry("type", type);
            fields.CopyTo(entries, 1);

            return _redis.GetDatabase().StreamAddAsync(
                AnalyticsKeys.Events,
                entries,
                maxLength: AnalyticsStream.MaxLength,
                useApproximateMaxLength: true,
                flags: CommandFlags.FireAndForget);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish analytics event {Type}", type);
            return Task.CompletedTask;
        }
    }
}
