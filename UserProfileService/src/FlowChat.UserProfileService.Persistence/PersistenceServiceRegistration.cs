using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FlowChat.Shared.Persistance.Auditing;

namespace FlowChat.UserProfileService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("UserProfileDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("UserProfileDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserProfileReadRepository, UserProfileReadRepository>();
        services.AddScoped<IUserProfileWriteRepository, UserProfileWriteRepository>();
        services.AddScoped<IEmailVerificationRequestWriteRepository, EmailVerificationRequestWriteRepository>();

        return services;
    }
}

