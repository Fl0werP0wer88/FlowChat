using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonPersistenceServices(configuration);

        services.AddScoped<IUnitOfWork, EfUnitOfWork<AppDbContext>>();

        return services;
    }
}

public static class ConsumerPersistenceServiceRegistration
{
    public static IServiceCollection AddConsumerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonPersistenceServices(configuration);
        services.AddScoped<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>());
        services.AddScoped<IConsumedOffsetCommitter>(serviceProvider =>
            serviceProvider.GetRequiredService<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>());

        return services;
    }
}

internal static class CommonPersistenceServiceRegistration
{
    public static void AddCommonPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("HarnessDb")));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("HarnessDb")),
            ServiceLifetime.Scoped);
    }
}
