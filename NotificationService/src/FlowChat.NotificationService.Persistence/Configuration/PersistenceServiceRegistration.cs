using FlowChat.Shared.Application;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Persistence.Repositories;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.NotificationService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonDbContextServices(configuration);

        services.AddScoped<IUnitOfWork, EfUnitOfWork<AppDbContext>>();
        services.AddNotificationRepositories();

        return services;
    }
}

public static class ConsumerPersistenceServiceRegistration
{
    public static IServiceCollection AddConsumerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonDbContextServices(configuration);

        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        services.AddNotificationRepositories();

        return services;
    }
}

internal static class CommonPersistenceServiceRegistration
{
    public static IServiceCollection AddCommonDbContextServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb")));

        return services;
    }

    public static IServiceCollection AddNotificationRepositories(this IServiceCollection services)
    {
        services.AddScoped<INotificationReadRepository, NotificationReadRepository>();
        services.AddScoped<INotificationWriteRepository, NotificationWriteRepository>();

        return services;
    }
}

