using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonDbContextServices(configuration);

        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();
        services.AddUserProfileRepositories();

        return services;
    }
}

public static class ConsumerPersistenceServiceRegistration
{
    public static IServiceCollection AddConsumerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCommonDbContextServices(configuration);

        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        services.AddUserProfileRepositories();

        return services;
    }
}

public static class OutboxPublisherPersistenceServiceRegistration
{
    public static IServiceCollection AddOutboxPublisherPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddCommonDbContextServices(configuration);
}

internal static class CommonPersistenceServiceRegistration
{
    public static IServiceCollection AddCommonDbContextServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("UserProfileDb")));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("UserProfileDb")),
            ServiceLifetime.Scoped);

        return services;
    }

    public static IServiceCollection AddUserProfileRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileReadRepository, UserProfileReadRepository>();
        services.AddScoped<IUserProfileWriteRepository, UserProfileWriteRepository>();
        services.AddScoped<IEmailVerificationProcessWriteRepository, EmailVerificationProcessWriteRepository>();

        return services;
    }
}
