using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FlowChat.Shared.Persistance.Auditing;

namespace FlowChat.SocialGraphService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("SocialGraphDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));

        services.AddScoped<IContactWriteRepository, ContactWriteRepository>();
        services.AddScoped<IContactReadRepository, ContactReadRepository>();
        services.AddScoped<IUserProfileReadModelRepository, UserProfileReadModelRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}

