using System.Globalization;
using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

public class AnalyticsConsumer : BackgroundService
{
    private static readonly TimeSpan DailyStatsTtl = TimeSpan.FromDays(35);
    private static readonly TimeSpan ActivePlayersTtl = TimeSpan.FromDays(90);
    private static readonly TimeSpan CountedTtl = TimeSpan.FromHours(1);
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ReclaimInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StaleConsumerAfter = TimeSpan.FromMinutes(10);
    private const int MaxApplyAttempts = 3;

    // Which counter in the daily stats hash each event type increments.
    private static readonly Dictionary<string, string> DailyStatsFields = new()
    {
        [AnalyticsEventTypes.PlayerLoggedIn] = "logins",
        [AnalyticsEventTypes.MatchCreated] = "matchesCreated",
        [AnalyticsEventTypes.PlayerJoinedMatch] = "playersJoined",
        [AnalyticsEventTypes.PlayerLeftMatch] = "playersLeft",
        [AnalyticsEventTypes.PlayerFinishedMatch] = "playersFinished",
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
        var groupReady = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!groupReady)
                {
                    await EnsureGroupExistsAsync(db);
                    groupReady = true;
                }

                var entries = await db.StreamReadGroupAsync(
                    AnalyticsKeys.Events, AnalyticsStream.GroupName, AnalyticsStream.ConsumerName, ">", count: 10);

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

    // Kept inside the retry loop: if Redis is unreachable at startup, this must
    // fail and be retried, never escape and take the whole host down with it.
    private static async Task EnsureGroupExistsAsync(IDatabase db)
    {
        try
        {
            await db.StreamCreateConsumerGroupAsync(
                AnalyticsKeys.Events, AnalyticsStream.GroupName, "0", createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
        }
    }

    private async Task ReclaimStaleAsync(IDatabase db)
    {
        if (DateTime.UtcNow - _lastReclaimUtc < ReclaimInterval)
            return;

        _lastReclaimUtc = DateTime.UtcNow;

        var result = await db.StreamAutoClaimAsync(
            AnalyticsKeys.Events,
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
        var consumers = await db.StreamConsumerInfoAsync(AnalyticsKeys.Events, AnalyticsStream.GroupName);

        foreach (var consumer in consumers)
        {
            if (consumer.Name == AnalyticsStream.ConsumerName)
                continue;

            if (consumer.PendingMessageCount > 0)
                continue;

            if (consumer.IdleTimeInMilliseconds < StaleConsumerAfter.TotalMilliseconds)
                continue;

            await db.StreamDeleteConsumerAsync(
                AnalyticsKeys.Events, AnalyticsStream.GroupName, consumer.Name);

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

            await db.StreamAcknowledgeAsync(AnalyticsKeys.Events, AnalyticsStream.GroupName, entry.Id);
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

        if (!DailyStatsFields.TryGetValue(type, out var statsField))
        {
            _logger.LogWarning("Unknown event type {Type} in {Id}, discarding", type, entry.Id);
            return true;
        }

        var day = DateOnly.FromDateTime(EventTimeUtc(entry));
        var dailyStatsKey = AnalyticsKeys.DailyStats(day);
        var activePlayersKey = AnalyticsKeys.ActivePlayers(day);

        long? activePlayerId = null;
        if (type == AnalyticsEventTypes.PlayerLoggedIn
            && fields.TryGetValue("playerId", out var raw)
            && long.TryParse(raw, out var playerId))
        {
            activePlayerId = playerId;
        }

        // Leaderboard points are whole numbers: adding decimals together in Redis
        // produces totals like 10.299999999999999.
        string? leaderboardPlayer = null;
        double leaderboardPoints = 0;
        if (type == AnalyticsEventTypes.PlayerFinishedMatch)
        {
            if (fields.TryGetValue("playerName", out var playerName)
                && playerName.Length > 0
                && fields.TryGetValue("score", out var rawScore)
                && double.TryParse(rawScore, NumberStyles.Float, CultureInfo.InvariantCulture, out var score))
            {
                leaderboardPlayer = playerName;
                leaderboardPoints = Math.Round(score, MidpointRounding.AwayFromZero);
            }
            else
            {
                _logger.LogWarning("Event {Id} is missing a player name or score, leaderboard not updated", entry.Id);
            }
        }

        // Every event shares the counted-events hash, so a transaction can also be
        // interrupted by unrelated activity on that hash, not only because this
        // event was already counted. Retry until it commits or the event really is
        // a duplicate, so a real event is never dropped as one by mistake.
        for (var attempt = 1; attempt <= MaxApplyAttempts; attempt++)
        {
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.HashNotExists(AnalyticsKeys.EventsCounted, entry.Id));
            _ = tran.HashSetAsync(AnalyticsKeys.EventsCounted, entry.Id, type);
            _ = tran.HashFieldExpireAsync(AnalyticsKeys.EventsCounted, new[] { entry.Id }, CountedTtl);

            _ = tran.HashIncrementAsync(dailyStatsKey, statsField);
            _ = tran.KeyExpireAsync(dailyStatsKey, DailyStatsTtl);

            if (activePlayerId is long id)
            {
                _ = tran.StringSetBitAsync(activePlayersKey, id, true);
                _ = tran.KeyExpireAsync(activePlayersKey, ActivePlayersTtl);
            }

            if (leaderboardPlayer is not null)
            {
                _ = tran.SortedSetIncrementAsync(
                    AnalyticsKeys.LeaderboardTotalScore, leaderboardPlayer, leaderboardPoints);
            }

            if (await tran.ExecuteAsync())
            {
                _logger.LogInformation("Applied {Type} to {Key}", type, dailyStatsKey);

                if (leaderboardPlayer is not null)
                {
                    _logger.LogInformation("Added {Points} points for {Player} to {Key}",
                        leaderboardPoints, leaderboardPlayer, AnalyticsKeys.LeaderboardTotalScore);
                }

                return true;
            }

            if (await db.HashExistsAsync(AnalyticsKeys.EventsCounted, entry.Id))
                return false;
        }

        throw new InvalidOperationException(
            $"Could not apply {entry.Id} after {MaxApplyAttempts} attempts");
    }

    private static DateTime EventTimeUtc(StreamEntry entry)
    {
        var id = entry.Id.ToString();
        var dash = id.IndexOf('-');
        var millis = long.Parse(dash < 0 ? id : id[..dash]);
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
    }
}
