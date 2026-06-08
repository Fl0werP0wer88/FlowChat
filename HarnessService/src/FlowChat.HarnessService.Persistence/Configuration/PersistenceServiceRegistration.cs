using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.HarnessService.Persistence.BulkUpsert.Projections;
using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("HarnessDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("HarnessDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);

        services.AddScoped<IProjectionTestBulkRepository, ProjectionTestBulkRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork<AppDbContext>>();

        return services;
    }
}
