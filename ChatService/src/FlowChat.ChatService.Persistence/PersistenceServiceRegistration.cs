using FlowChat.Application.Abstractions;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.ChatService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ChatDb")));
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("ChatDb")),
            ServiceLifetime.Scoped);
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ChatDb")));
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("ChatDb")),
            ServiceLifetime.Scoped);

        return services;
    }
}
