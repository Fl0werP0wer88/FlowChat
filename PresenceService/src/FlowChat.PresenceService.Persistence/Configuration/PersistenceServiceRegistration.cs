using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Persistence.Repositories;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.PresenceService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextServices(configuration);

        services.AddPresenceRepositories();
        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();

        return services;
    }
}

public static class ConsumerPersistenceServiceRegistration
{
    public static IServiceCollection AddConsumerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextServices(configuration);

        services.AddPresenceRepositories();
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
        => services.AddDbContextServices(configuration);
}

internal static class CommonPersistenceServiceRegistration
{
    public static IServiceCollection AddDbContextServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("PresenceDb")));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("PresenceDb")),
            ServiceLifetime.Scoped);

        return services;
    }

    public static IServiceCollection AddPresenceRepositories(this IServiceCollection services)
    {
        services.AddScoped<IContactObserverProjectionReadRepository, ContactObserverProjectionReadRepository>();
        services.AddScoped<IUserPresencePreferencesReadRepository, UserPresencePreferencesReadRepository>();
        services.AddScoped<IUserPresencePreferencesWriteRepository, UserPresencePreferencesWriteRepository>();

        return services;
    }
}
