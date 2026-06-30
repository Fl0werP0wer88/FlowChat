using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.SocialGraphService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextServices(configuration);

        services.AddSocialGraphRepositories();
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

        services.AddSocialGraphRepositories();
        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();

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
            options.UseNpgsql(configuration.GetConnectionString("SocialGraphDb")));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("SocialGraphDb")),
            ServiceLifetime.Scoped);

        return services;
    }

    public static IServiceCollection AddSocialGraphRepositories(this IServiceCollection services)
    {
        services.AddScoped<IContactWriteRepository, ContactWriteRepository>();
        services.AddScoped<IContactReadRepository, ContactReadRepository>();
        services.AddScoped<IUserProfileProjectionReadRepository, UserProfileProjectionReadRepository>();

        return services;
    }
}

