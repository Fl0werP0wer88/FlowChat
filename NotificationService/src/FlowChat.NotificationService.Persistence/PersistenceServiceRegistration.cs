using FlowChat.Shared.Application;
using FlowChat.NotificationService.Application.Contracts.Persistence;
using FlowChat.NotificationService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FlowChat.Shared.Persistance.Auditing;

namespace FlowChat.NotificationService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("NotificationDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<INotificationReadRepository, NotificationReadRepository>();
        services.AddScoped<INotificationWriteRepository, NotificationWriteRepository>();

        return services;
    }
}

