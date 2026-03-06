using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Persistence.Auditing;
using FlowChat.AuthService.Persistence.UnitOfWork;
using FlowChat.AuthService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.EntityFrameworkCore;

namespace FlowChat.AuthService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddAPIPersistenceServices(
                            this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<IUnitOfWork, AppDbContextUnitOfWork>();

        services.AddDbContextWithWolverineIntegration<AppDbContext>((serviceProvider, options) =>
            ConfigureDbContext(serviceProvider, options, configuration));
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("AuthDb")),
            ServiceLifetime.Scoped);

        services.AddScoped<IIdentityRepository, IdentityRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
                            this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddDbContextWithWolverineIntegration<AppDbContext>((serviceProvider, options) =>
            ConfigureDbContext(serviceProvider, options, configuration));
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("AuthDb")),
            ServiceLifetime.Scoped);

        return services;
    }

    private static void ConfigureDbContext(
        IServiceProvider serviceProvider,
        DbContextOptionsBuilder options,
        IConfiguration configuration)
    {
        options.UseNpgsql(configuration.GetConnectionString("AuthDb"));
        options.AddInterceptors(
            serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
    }
}
