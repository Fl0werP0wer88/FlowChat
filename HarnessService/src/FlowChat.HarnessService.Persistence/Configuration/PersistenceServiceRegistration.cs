using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Persistence.Entities.Projections;
using FlowChat.HarnessService.Persistence.BulkUpsert.Projections;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.Shared.Persistance.ProjectionBulk;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Persistence;

public static class PersistenceServiceRegistration
{
    // API no longer runs projection bulk commands (the Consumer worker calls them in-process now),
    // but MediatR still registers the handler host-wide, so IUnitOfWork/IProjectionBulkRepository
    // must stay resolvable here to satisfy DI validation on startup. Plain EfUnitOfWork is enough since
    // the API host never connects to Silverback/Kafka and has no ISilverbackContext to enlist.
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonPersistenceServices(configuration);

        services.AddScoped<ProjectionTestBulkEntityFactory>();
        services.AddScoped<
            IProjectionBulkEntityFactory<ProjectionCommandItem, ProjectionTestDto, ProjectionTestEntity>,
            ProjectionTestBulkEntityFactory>();
        services.AddScoped<
            IProjectionBulkRepository<ProjectionCommandItem>,
            ProjectionBulkRepository<AppDbContext, ProjectionCommandItem, ProjectionTestDto, ProjectionTestEntity, ProjectionTestBulkEntityFactory>>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork<AppDbContext>>();

        return services;
    }

    public static IServiceCollection AddConsumerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonPersistenceServices(configuration);

        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();

        return services;
    }

    private static void AddCommonPersistenceServices(
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
    }
}
