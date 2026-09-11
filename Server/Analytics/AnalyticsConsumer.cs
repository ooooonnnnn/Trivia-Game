using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

public class AnalyticsConsumer : BackgroundService
{
    private static readonly TimeSpan FunnelTtl = TimeSpan.FromDays(35);
    private static readonly TimeSpan ActiveTtl = TimeSpan.FromDays(90);
    private static readonly TimeSpan DedupeTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ReclaimInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StaleConsumerAfter = TimeSpan.FromMinutes(10);

    private static readonly Dictionary<string, string> FunnelFields = new()
    {
        ["player.login"] = "logins",
        ["match.created"] = "matchesCreated",
        ["player.joined"] = "playersJoined",
        ["player.left"] = "playersLeft",
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnalyticsConsumer> _logger;
    private DateTime _lastReclaimUtc = DateTime.MinValue;

    public AnalyticsConsumer(IConnectionMultiplexer redis, ILogger<AnalyticsConsumer> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();

        try
        {
            await db.StreamCreateConsumerGroupAsync(AnalyticsStream.Key, AnalyticsStream.GroupName, "0", createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entries = await db.StreamReadGroupAsync(
                    AnalyticsStream.Key, AnalyticsStream.GroupName, AnalyticsStream.ConsumerName, ">", count: 10);

                if (entries.Length == 0)
                {
                    await ReclaimStaleAsync(db);
                    await Task.Delay(IdleDelay, stoppingToken);
                    continue;
                }

                foreach (var entry in entries)
                {
                    await ProcessAsync(db, entry);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics read loop failed, retrying");
                await Task.Delay(ErrorDelay, stoppingToken);
            }
        }
    }

    private async Task ReclaimStaleAsync(IDatabase db)
    {
        if (DateTime.UtcNow - _lastReclaimUtc < ReclaimInterval)
            return;

        _lastReclaimUtc = DateTime.UtcNow;

        var result = await db.StreamAutoClaimAsync(
            AnalyticsStream.Key,
            AnalyticsStream.GroupName,
            AnalyticsStream.ConsumerName,
            (long)StaleAfter.TotalMilliseconds,
            "0-0",
            count: 10);

        if (result.ClaimedEntries.Length > 0)
        {
            _logger.LogWarning("Reclaimed {Count} stale entries", result.ClaimedEntries.Length);

            foreach (var entry in result.ClaimedEntries)
            {
                await ProcessAsync(db, entry);
            }
        }

        await RemoveDeadConsumersAsync(db);
    }

    private async Task RemoveDeadConsumersAsync(IDatabase db)
    {
        var consumers = await db.StreamConsumerInfoAsync(AnalyticsStream.Key, AnalyticsStream.GroupName);

        foreach (var consumer in consumers)
        {
            if (consumer.Name == AnalyticsStream.ConsumerName)
                continue;

            if (consumer.PendingMessageCount > 0)
                continue;

            if (consumer.IdleTimeInMilliseconds < StaleConsumerAfter.TotalMilliseconds)
                continue;

            await db.StreamDeleteConsumerAsync(
                AnalyticsStream.Key, AnalyticsStream.GroupName, consumer.Name);

            _logger.LogInformation("Removed dead consumer {Consumer}", consumer.Name);
        }
    }

    private async Task ProcessAsync(IDatabase db, StreamEntry entry)
    {
        try
        {
            var applied = await ApplyAsync(db, entry);

            if (!applied)
            {
                _logger.LogInformation("Skipped duplicate {Id}", entry.Id);
            }

            await db.StreamAcknowledgeAsync(AnalyticsStream.Key, AnalyticsStream.GroupName, entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process {Id}, leaving it pending", entry.Id);
        }
    }

    private async Task<bool> ApplyAsync(IDatabase db, StreamEntry entry)
    {
        var fields = entry.Values.ToDictionary(
            v => v.Name.ToString(), v => v.Value.ToString());

        if (!fields.TryGetValue("type", out var type))
        {
            _logger.LogWarning("Event {Id} has no type field, discarding", entry.Id);
            return true;
        }

        var day = EventTimeUtc(entry).ToString("yyyy-MM-dd");
        var dedupeKey = (RedisKey)$"dedupe:{entry.Id}";

        if (!FunnelFields.TryGetValue(type, out var funnelField))
        {
            _logger.LogWarning("Unknown event type {Type} in {Id}, discarding", type, entry.Id);
            return true;
        }

        var tran = db.CreateTransaction();
        tran.AddCondition(Condition.KeyNotExists(dedupeKey));
        _ = tran.StringSetAsync(dedupeKey, "1", DedupeTtl);

        var funnelKey = (RedisKey)$"funnel:{day}";
        _ = tran.HashIncrementAsync(funnelKey, funnelField);
        _ = tran.KeyExpireAsync(funnelKey, FunnelTtl);

        if (type == "player.login"
            && fields.TryGetValue("playerId", out var raw)
            && long.TryParse(raw, out var playerId))
        {
            var activeKey = (RedisKey)$"active:{day}";
            _ = tran.StringSetBitAsync(activeKey, playerId, true);
            _ = tran.KeyExpireAsync(activeKey, ActiveTtl);
        }

        var committed = await tran.ExecuteAsync();

        if (committed)
        {
            _logger.LogInformation("Applied {Type} to {Day}", type, day);
        }

        return committed;
    }

    private static DateTime EventTimeUtc(StreamEntry entry)
    {
        var id = entry.Id.ToString();
        var dash = id.IndexOf('-');
        var millis = long.Parse(dash < 0 ? id : id[..dash]);
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
    }
}
