using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDbContextServices(services, configuration);

        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();
        services.AddUserProfileRepositories();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        AddDbContextServices(services, configuration);

        services.AddScoped<IUnitOfWork, SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        services.AddUserProfileRepositories();

        return services;
    }

    private static void AddDbContextServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("UserProfileDb")));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("UserProfileDb")),
            ServiceLifetime.Scoped);
    }

    private static IServiceCollection AddUserProfileRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserProfileReadRepository, UserProfileReadRepository>();
        services.AddScoped<IUserProfileWriteRepository, UserProfileWriteRepository>();
        services.AddScoped<IEmailVerificationProcessWriteRepository, EmailVerificationProcessWriteRepository>();

        return services;
    }
}
