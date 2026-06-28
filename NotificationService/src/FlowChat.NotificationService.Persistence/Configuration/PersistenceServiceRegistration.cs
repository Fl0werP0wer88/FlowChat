using FlowChat.Shared.Application;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Persistence.Repositories;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.NotificationService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDbContextServices(services, configuration);

        services.AddScoped<IUnitOfWork, EfUnitOfWork<AppDbContext>>();
        services.AddNotificationRepositories();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDbContextServices(services, configuration);

        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        services.AddNotificationRepositories();

        return services;
    }

    private static void AddDbContextServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb")));
    }

    private static IServiceCollection AddNotificationRepositories(this IServiceCollection services)
    {
        services.AddScoped<INotificationReadRepository, NotificationReadRepository>();
        services.AddScoped<INotificationWriteRepository, NotificationWriteRepository>();

        return services;
    }
}

