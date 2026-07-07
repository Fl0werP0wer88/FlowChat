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
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("RealtimeDb")));

        services.AddScoped<IRealtimeGroupMembershipReadModelRepository, RealtimeGroupMembershipReadModelRepository>();

        return services;
    }
}
