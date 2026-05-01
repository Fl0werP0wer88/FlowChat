using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;
using FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.Auditing;
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
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddPostgresDbUpdateExceptionClassifier(options =>
        {
            options.UniqueConstraintNamesByIdempotencyConflictKey[AddEmailCommand.IdempotencyConflictKey] =
                ["PK_Emails"];
            options.UniqueConstraintNamesByIdempotencyConflictKey[AddPhoneCommand.IdempotencyConflictKey] =
                ["PK_Phones"];
        });
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("UserProfileDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("UserProfileDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);

        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();
        services.AddScoped<IUserProfileReadRepository, UserProfileReadRepository>();
        services.AddScoped<IUserProfileWriteRepository, UserProfileWriteRepository>();
        services.AddScoped<IEmailVerificationRequestWriteRepository, EmailVerificationRequestWriteRepository>();

        return services;
    }
}
