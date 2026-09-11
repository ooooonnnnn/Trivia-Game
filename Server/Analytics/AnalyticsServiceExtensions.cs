using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

public static class AnalyticsServiceExtensions
{
    public static IServiceCollection AddAnalytics(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"]
                               ?? throw new InvalidOperationException(
                                   "Redis:ConnectionString is not configured. Set it with dotnet user-secrets.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        services.AddSingleton<AnalyticsPublisher>();
        services.AddHostedService<AnalyticsConsumer>();

        return services;
    }
}
