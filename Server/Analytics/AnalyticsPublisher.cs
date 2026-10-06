using System.Globalization;
using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

public class AnalyticsPublisher
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<AnalyticsPublisher> _logger;

    // redis is null when analytics is disabled.
    public AnalyticsPublisher(IConnectionMultiplexer? redis, ILogger<AnalyticsPublisher> logger)
    {
        _redis = redis;
        _logger = logger;

        if (_redis is null)
        {
            _logger.LogWarning("Redis:ConnectionString is not set, so analytics is disabled");
        }
    }

    public Task PlayerLoggedInAsync(int playerId) =>
        PublishAsync(AnalyticsEventTypes.PlayerLoggedIn,
            new NameValueEntry(AnalyticsEventFields.PlayerId, playerId));

    public Task MatchCreatedAsync(int matchId) =>
        PublishAsync(AnalyticsEventTypes.MatchCreated,
            new NameValueEntry(AnalyticsEventFields.MatchId, matchId));

    public Task PlayerJoinedMatchAsync(int matchId, int playerId) =>
        PublishAsync(AnalyticsEventTypes.PlayerJoinedMatch,
            new NameValueEntry(AnalyticsEventFields.MatchId, matchId),
            new NameValueEntry(AnalyticsEventFields.PlayerId, playerId));

    public Task PlayerLeftMatchAsync(int matchId, int playerId) =>
        PublishAsync(AnalyticsEventTypes.PlayerLeftMatch,
            new NameValueEntry(AnalyticsEventFields.MatchId, matchId),
            new NameValueEntry(AnalyticsEventFields.PlayerId, playerId));

    // Score uses one decimal, same as the results screen.
    public Task PlayerFinishedMatchAsync(int matchId, int playerId, string playerName, float score) =>
        PublishAsync(AnalyticsEventTypes.PlayerFinishedMatch,
            new NameValueEntry(AnalyticsEventFields.MatchId, matchId),
            new NameValueEntry(AnalyticsEventFields.PlayerId, playerId),
            new NameValueEntry(AnalyticsEventFields.PlayerName, playerName),
            new NameValueEntry(AnalyticsEventFields.Score, score.ToString("0.#", CultureInfo.InvariantCulture)));

    // Fire-and-forget and never throws, so analytics can't slow down or break the game.
    private Task PublishAsync(string type, params NameValueEntry[] fields)
    {
        if (_redis is null)
            return Task.CompletedTask;

        try
        {
            NameValueEntry[] entry = [new(AnalyticsEventFields.Type, type), .. fields];

            return _redis.GetDatabase().StreamAddAsync(
                AnalyticsKeys.Events,
                entry,
                maxLength: AnalyticsKeys.MaxEventsKept,
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
