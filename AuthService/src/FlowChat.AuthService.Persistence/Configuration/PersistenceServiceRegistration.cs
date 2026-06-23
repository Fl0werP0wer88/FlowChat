using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Commands.RegisterUser;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.Auditing;
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
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddPostgresDbUpdateExceptionClassifier(options =>
        {
            options.UniqueConstraintNamesByIdempotencyConflictKey[RegisterUserCommand.IdempotencyConflictKey] =
                ["PK_Accounts"];
        });
        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("AuthDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("AuthDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);

        services.AddScoped<IAccountRepository, AccountRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
                            this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddPostgresDbUpdateExceptionClassifier();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("AuthDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("AuthDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);

        return services;
    }
}

