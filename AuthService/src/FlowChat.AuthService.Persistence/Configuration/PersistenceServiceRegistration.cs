using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.AuthService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();

        services.AddCommonDbContextServices(configuration);

        services.AddScoped<IAccountRepository, AccountRepository>();

        return services;
    }
}

public static class ConsumerPersistenceServiceRegistration
{
    public static IServiceCollection AddConsumerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();

        services.AddCommonDbContextServices(configuration);

        services.AddScoped<IAccountRepository, AccountRepository>();

        return services;
    }
}

public static class OutboxPublisherPersistenceServiceRegistration
{
    public static IServiceCollection AddOutboxPublisherPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddCommonDbContextServices(configuration);
}

internal static class CommonPersistenceServiceRegistration
{
    public static IServiceCollection AddCommonDbContextServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("AuthDb")));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("AuthDb")),
            ServiceLifetime.Scoped);

        return services;
    }
}

