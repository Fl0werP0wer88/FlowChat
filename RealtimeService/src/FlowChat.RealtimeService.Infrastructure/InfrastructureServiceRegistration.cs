using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.RealtimeService.Infrastructure.RealtimeConnections;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton(sp => sp.GetRequiredService<IApiSettingsManager>().GetRealtimeConnectionsSettings());
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(sp.GetRequiredService<RealtimeConnectionsSettings>().RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.TryAddSingleton<IActiveRealtimeConnectionTracker, InMemoryActiveRealtimeConnectionTracker>();
        services.TryAddSingleton<IRealtimeConnectionRegistry, RedisRealtimeConnectionRegistry>();
        services.AddHostedService<RealtimeConnectionRefreshBackgroundService>();

        return services;
    }
}
