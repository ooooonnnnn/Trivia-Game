using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

public class AnalyticsPublisher
{
    private readonly IConnectionMultiplexer _redis;

    public AnalyticsPublisher(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task PlayerLoggedInAsync(int playerId) =>
        PublishAsync("player.login", new NameValueEntry("playerId", playerId));

    public Task MatchCreatedAsync(int matchId) =>
        PublishAsync("match.created", new NameValueEntry("matchId", matchId));

    public Task PlayerJoinedMatchAsync(int matchId, int playerId) =>
        PublishAsync("player.joined",
            new NameValueEntry("matchId", matchId),
            new NameValueEntry("playerId", playerId));

    public Task PlayerLeftMatchAsync(int matchId, int playerId) =>
        PublishAsync("player.left",
            new NameValueEntry("matchId", matchId),
            new NameValueEntry("playerId", playerId));

    private Task PublishAsync(string type, params NameValueEntry[] fields)
    {
        var entries = new NameValueEntry[fields.Length + 1];
        entries[0] = new NameValueEntry("type", type);
        fields.CopyTo(entries, 1);

        return _redis.GetDatabase().StreamAddAsync(
            AnalyticsStream.Key,
            entries,
            maxLength: AnalyticsStream.MaxLength,
            useApproximateMaxLength: true);
    }
}
