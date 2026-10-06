using StackExchange.Redis;

namespace Trivia_Game_Server.Analytics;

public static class AnalyticsServiceExtensions
{
    public static IServiceCollection AddAnalytics(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["Redis:ConnectionString"];

        // Analytics is optional: without a connection string the game runs without it.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddSingleton(sp => new AnalyticsPublisher(
                null, sp.GetRequiredService<ILogger<AnalyticsPublisher>>()));
            return services;
        }

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
        services.AddSingleton<AnalyticsPublisher>();
        services.AddHostedService<AnalyticsConsumer>();

        return services;
    }
}
