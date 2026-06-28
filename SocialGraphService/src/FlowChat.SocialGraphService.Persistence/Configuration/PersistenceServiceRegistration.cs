using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.SocialGraphService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextServices(configuration);

        services.AddScoped<IContactWriteRepository, ContactWriteRepository>();
        services.AddScoped<IContactReadRepository, ContactReadRepository>();
        services.AddScoped<IUserProfileProjectionReadRepository, UserProfileProjectionReadRepository>();
        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextServices(configuration);

        services.AddScoped<IContactWriteRepository, ContactWriteRepository>();
        services.AddScoped<IContactReadRepository, ContactReadRepository>();
        services.AddScoped<IUserProfileProjectionReadRepository, UserProfileProjectionReadRepository>();
        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();

        return services;
    }

    private static IServiceCollection AddDbContextServices(
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
}

