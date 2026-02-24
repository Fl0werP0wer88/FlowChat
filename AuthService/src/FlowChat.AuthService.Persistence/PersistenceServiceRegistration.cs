using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Persistence.Auditing;
using FlowChat.AuthService.Persistence.Outbox;
using FlowChat.AuthService.Persistence.UnitOfWork;
using FlowChat.Messaging.Contracts.AuthService.Events;
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
        services.AddOptions<UserCreatedProducerOptions>();
        services.AddOptions<UserEmailVerificationRequestedOutboxOptions>();
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<InsertOutboxMessagesInterceptor>();
        services.AddScoped<IUnitOfWork, AppDbContextUnitOfWork>();
        services.AddScoped<
            IOutboxRepository<UserEmailVerificationRequested>,
            OutboxRepository<UserEmailVerificationRequested, UserEmailVerificationRequestedOutboxOptions>>();

        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AuthDb"));
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>());
        });

        services.AddScoped<IIdentityRepository, IdentityRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
                            this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<InsertOutboxMessagesInterceptor>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(configuration.GetConnectionString("AuthDb"));
            options.AddInterceptors(
                serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>(),
                serviceProvider.GetRequiredService<InsertOutboxMessagesInterceptor>());
        });

        return services;
    }
}
