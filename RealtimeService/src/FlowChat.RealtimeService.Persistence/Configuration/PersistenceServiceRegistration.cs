using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonPersistenceServices(configuration);

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

        return services;
    }
}

internal static class CommonPersistenceServiceRegistration
{
    public static void AddCommonPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("RealtimeDb")));

        services.AddScoped<IRealtimeGroupMembershipReadModelRepository, RealtimeGroupMembershipReadModelRepository>();
        services.AddScoped<IRealtimeGroupMembershipVersionTrackerRepository, RealtimeGroupMembershipVersionTrackerRepository>();
    }
}
