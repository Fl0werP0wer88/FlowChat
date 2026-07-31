using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Persistence.Repositories;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // No IUnitOfWork here: the API host doesn't wire Silverback (no ISilverbackContext to back
        // SilverbackEfUnitOfWork), and none of the handlers registered on this host need it today.
        services.AddCommonPersistenceServices(configuration);

        return services;
    }
}

public static class ConsumerPersistenceServiceRegistration
{
    public static IServiceCollection AddConsumerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonPersistenceServices(configuration);
        services.AddScoped<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>());
        services.AddScoped<IConsumedOffsetCommitter>(serviceProvider =>
            serviceProvider.GetRequiredService<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>());

        return services;
    }
}

public static class OutboxPublisherPersistenceServiceRegistration
{
    public static IServiceCollection AddOutboxPublisherPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonPersistenceServices(configuration);
        return services;
    }
}

internal static class CommonPersistenceServiceRegistration
{
    public static void AddCommonPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("RealtimeDb")));
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("RealtimeDb")),
            ServiceLifetime.Scoped);

        services.AddScoped<IRealtimeGroupMembershipReadModelRepository, RealtimeGroupMembershipReadModelRepository>();
        services.AddScoped<IRealtimeGroupMembershipRevisionTrackerRepository, RealtimeGroupMembershipRevisionTrackerRepository>();
    }
}
