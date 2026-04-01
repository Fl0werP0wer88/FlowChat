using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FlowChat.Shared.Persistance.Auditing;

namespace FlowChat.ChatService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);
        services.AddScoped<IChatMessageWriteRepository, ChatMessageWriteRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);

        return services;
    }
}

