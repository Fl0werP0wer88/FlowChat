using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Persistence.Repositories;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.PresenceService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextServices(configuration);

        services.AddScoped<IContactObserverProjectionReadRepository, ContactObserverProjectionReadRepository>();
        services.AddScoped<IUserPresencePreferencesReadRepository, UserPresencePreferencesReadRepository>();
        services.AddScoped<IUserPresencePreferencesWriteRepository, UserPresencePreferencesWriteRepository>();
        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextServices(configuration);

        services.AddScoped<IContactObserverProjectionReadRepository, ContactObserverProjectionReadRepository>();
        services.AddScoped<IUserPresencePreferencesReadRepository, UserPresencePreferencesReadRepository>();
        services.AddScoped<IUserPresencePreferencesWriteRepository, UserPresencePreferencesWriteRepository>();
        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();

        return services;
    }

    private static IServiceCollection AddDbContextServices(
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
}
