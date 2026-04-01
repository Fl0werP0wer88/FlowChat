using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Persistence.UnitOfWork;
using FlowChat.AuthService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddAPIPersistenceServices(
                            this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, AppDbContextUnitOfWork>();

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AuthDb"));
        });
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("AuthDb")),
            ServiceLifetime.Scoped);

        services.AddScoped<IIdentityRepository, IdentityRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
                            this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AuthDb"));
        });
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("AuthDb")),
            ServiceLifetime.Scoped);

        return services;
    }
}

