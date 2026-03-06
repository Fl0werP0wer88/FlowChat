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
        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<IUnitOfWork, AppDbContextUnitOfWork>();

        services.AddDbContextWithWolverineIntegration<AppDbContext>((serviceProvider, options) =>
            ConfigureDbContext(serviceProvider, options, configuration));

        services.AddScoped<IIdentityRepository, IdentityRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
                            this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntitySaveChangesInterceptor>();
        services.AddDbContextWithWolverineIntegration<AppDbContext>((serviceProvider, options) =>
            ConfigureDbContext(serviceProvider, options, configuration));

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
