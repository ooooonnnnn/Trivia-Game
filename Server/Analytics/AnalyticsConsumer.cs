using System.Globalization;
using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

// Reads events from trivia:events in the background and counts them into the
// daily stats, active players and leaderboard keys described in AnalyticsKeys.
public class AnalyticsConsumer : BackgroundService
{
    // Redis stream positions, named so the symbols don't need decoding.
    private const string NewEventsOnly = ">";    // events nobody in the group has been given yet
    private const string FromFirstEvent = "0-0"; // the very start of the stream

    private const int BatchSize = 10;
    private const int MaxCountAttempts = 3;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan RecoveryInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan StrandedAfter = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan DeadConsumerAfter = TimeSpan.FromMinutes(10);

    // Which counter in the daily stats hash each event type adds 1 to.
    private static readonly Dictionary<string, string> DailyCounterByEventType = new()
    {
        [AnalyticsEventTypes.PlayerLoggedIn] = "logins",
        [AnalyticsEventTypes.MatchCreated] = "matchesCreated",
        [AnalyticsEventTypes.PlayerJoinedMatch] = "playersJoined",
        [AnalyticsEventTypes.PlayerLeftMatch] = "playersLeft",
        [AnalyticsEventTypes.PlayerFinishedMatch] = "playersFinished",
    };

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AnalyticsConsumer> _logger;
    private DateTime _lastRecoveryUtc = DateTime.MinValue;

    public AnalyticsConsumer(IConnectionMultiplexer redis, ILogger<AnalyticsConsumer> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var db = _redis.GetDatabase();
        var groupExists = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!groupExists)
                {
                    await EnsureGroupExistsAsync(db);
                    groupExists = true;
                }

                var newEvents = await db.StreamReadGroupAsync(
                    AnalyticsKeys.Events, AnalyticsStream.GroupName, AnalyticsStream.ConsumerName,
                    NewEventsOnly, count: BatchSize);

                if (newEvents.Length == 0)
                {
                    await RecoverStrandedEventsAsync(db);
                    await Task.Delay(PollInterval, stoppingToken);
                    continue;
                }

                foreach (var entry in newEvents)
                {
                    await HandleEventAsync(db, entry);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Analytics read loop failed, retrying");
                await Task.Delay(RetryDelay, stoppingToken);
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
                AnalyticsKeys.Events, AnalyticsStream.GroupName, FromFirstEvent, createStream: true);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
        {
            // The group already exists, which is the normal case after the first start.
        }
    }

    // An event that was handed out but never acknowledged, because a consumer
    // crashed partway through, is "stranded". Every RecoveryInterval, take over
    // any that have waited longer than StrandedAfter and count them.
    private async Task RecoverStrandedEventsAsync(IDatabase db)
    {
        if (DateTime.UtcNow - _lastRecoveryUtc < RecoveryInterval)
            return;

        _lastRecoveryUtc = DateTime.UtcNow;

        var result = await db.StreamAutoClaimAsync(
            AnalyticsKeys.Events,
            AnalyticsStream.GroupName,
            AnalyticsStream.ConsumerName,
            (long)StrandedAfter.TotalMilliseconds,
            FromFirstEvent,
            count: BatchSize);

        if (result.ClaimedEntries.Length > 0)
        {
            _logger.LogWarning("Recovered {Count} stranded events", result.ClaimedEntries.Length);

            foreach (var entry in result.ClaimedEntries)
            {
                await HandleEventAsync(db, entry);
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

            if (consumer.IdleTimeInMilliseconds < DeadConsumerAfter.TotalMilliseconds)
                continue;

            await db.StreamDeleteConsumerAsync(
                AnalyticsKeys.Events, AnalyticsStream.GroupName, consumer.Name);

            _logger.LogInformation("Removed dead consumer {Consumer}", consumer.Name);
        }
    }

    // Counts one event, then acknowledges it so Redis stops holding it as pending.
    // If counting fails it is not acknowledged, so it can be recovered and retried.
    private async Task HandleEventAsync(IDatabase db, StreamEntry entry)
    {
        try
        {
            var counted = await CountEventAsync(db, entry);

            if (!counted)
            {
                _logger.LogInformation("Event {Id} was already counted, skipping", entry.Id);
            }

            await db.StreamAcknowledgeAsync(AnalyticsKeys.Events, AnalyticsStream.GroupName, entry.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to count event {Id}, leaving it to be retried", entry.Id);
        }
    }

    // Adds one event to the stats. Returns false only if it had already been counted.
    private async Task<bool> CountEventAsync(IDatabase db, StreamEntry entry)
    {
        var fields = entry.Values.ToDictionary(v => v.Name.ToString(), v => v.Value.ToString());

        if (!fields.TryGetValue(AnalyticsEventFields.Type, out var type))
        {
            _logger.LogWarning("Event {Id} has no type, discarding", entry.Id);
            return true;
        }

        if (!DailyCounterByEventType.TryGetValue(type, out var dailyCounter))
        {
            _logger.LogWarning("Event {Id} has unknown type {Type}, discarding", entry.Id, type);
            return true;
        }

        var day = DateOnly.FromDateTime(EventTimeUtc(entry));
        var activePlayerId = PlayerToMarkActive(type, fields);
        var leaderboardEntry = LeaderboardEntryFor(type, fields, entry.Id);

        // Every event shares the counted-events hash, so a transaction can also be
        // interrupted by unrelated activity on that hash, not only because this
        // event was already counted. Retry until it goes through or the event
        // really is a duplicate, so a real event is never dropped as one by mistake.
        for (var attempt = 1; attempt <= MaxCountAttempts; attempt++)
        {
            // Everything in the transaction happens together, or not at all.
            var transaction = db.CreateTransaction();
            transaction.AddCondition(Condition.HashNotExists(AnalyticsKeys.EventsCounted, entry.Id));

            MarkAsCounted(transaction, entry.Id, type);
            AddToDailyStats(transaction, day, dailyCounter);

            if (activePlayerId is long playerId)
                MarkPlayerActive(transaction, day, playerId);

            if (leaderboardEntry is not null)
                AddLeaderboardPoints(transaction, leaderboardEntry);

            if (await transaction.ExecuteAsync())
            {
                _logger.LogInformation("Counted {Type} into {Key}", type, AnalyticsKeys.DailyStats(day));

                if (leaderboardEntry is not null)
                {
                    _logger.LogInformation("Added {Points} points for {Player} to {Key}",
                        leaderboardEntry.Points, leaderboardEntry.PlayerName, AnalyticsKeys.LeaderboardTotalScore);
                }

                return true;
            }

            if (await db.HashExistsAsync(AnalyticsKeys.EventsCounted, entry.Id))
                return false;
        }

        throw new InvalidOperationException(
            $"Could not count event {entry.Id} after {MaxCountAttempts} attempts");
    }

    // Logging in is what marks a player as active for the day.
    private static long? PlayerToMarkActive(string type, Dictionary<string, string> fields)
    {
        if (type == AnalyticsEventTypes.PlayerLoggedIn
            && fields.TryGetValue(AnalyticsEventFields.PlayerId, out var rawPlayerId)
            && long.TryParse(rawPlayerId, out var playerId))
        {
            return playerId;
        }

        return null;
    }

    // Finishing a match adds the score to the leaderboard, as whole points:
    // adding decimals together in Redis produces totals like 10.299999999999999.
    private LeaderboardEntry? LeaderboardEntryFor(
        string type, Dictionary<string, string> fields, RedisValue eventId)
    {
        if (type != AnalyticsEventTypes.PlayerFinishedMatch)
            return null;

        if (fields.TryGetValue(AnalyticsEventFields.PlayerName, out var playerName)
            && playerName.Length > 0
            && fields.TryGetValue(AnalyticsEventFields.Score, out var rawScore)
            && double.TryParse(rawScore, NumberStyles.Float, CultureInfo.InvariantCulture, out var score))
        {
            return new LeaderboardEntry(playerName, Math.Round(score, MidpointRounding.AwayFromZero));
        }

        _logger.LogWarning("Event {Id} is missing a player name or score, leaderboard not updated", eventId);
        return null;
    }

    // The methods below queue commands in the transaction rather than awaiting
    // them: they all run together when the transaction executes.

    private static void MarkAsCounted(ITransaction transaction, RedisValue eventId, string type)
    {
        _ = transaction.HashSetAsync(AnalyticsKeys.EventsCounted, eventId, type);
        _ = transaction.HashFieldExpireAsync(
            AnalyticsKeys.EventsCounted, [eventId], AnalyticsKeys.EventsCountedLifetime);
    }

    private static void AddToDailyStats(ITransaction transaction, DateOnly day, string counter)
    {
        var key = AnalyticsKeys.DailyStats(day);
        _ = transaction.HashIncrementAsync(key, counter);
        _ = transaction.KeyExpireAsync(key, AnalyticsKeys.DailyStatsLifetime);
    }

    private static void MarkPlayerActive(ITransaction transaction, DateOnly day, long playerId)
    {
        var key = AnalyticsKeys.ActivePlayers(day);
        _ = transaction.StringSetBitAsync(key, playerId, true);
        _ = transaction.KeyExpireAsync(key, AnalyticsKeys.ActivePlayersLifetime);
    }

    private static void AddLeaderboardPoints(ITransaction transaction, LeaderboardEntry entry)
    {
        _ = transaction.SortedSetIncrementAsync(
            AnalyticsKeys.LeaderboardTotalScore, entry.PlayerName, entry.Points);
    }

    private static DateTime EventTimeUtc(StreamEntry entry)
    {
        var id = entry.Id.ToString();
        var dash = id.IndexOf('-');
        var millis = long.Parse(dash < 0 ? id : id[..dash]);
        return DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime;
    }

    private sealed record LeaderboardEntry(string PlayerName, double Points);
}
