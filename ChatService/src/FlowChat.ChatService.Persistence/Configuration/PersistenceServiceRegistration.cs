using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.ChatService.Persistence;

public static class PersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddPostgresDbUpdateExceptionClassifier();
        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);
        services.AddScoped<IChatMessageReadRepository, ChatMessageReadRepository>();
        services.AddScoped<IChatMessageWriteRepository, ChatMessageWriteRepository>();
        services.AddScoped<IConversationParticipantReadRepository, ConversationParticipantReadRepository>();
        services.AddScoped<IGroupConversationWriteRepository, GroupConversationWriteRepository>();
        services.AddScoped<IDuetConversationReadRepository, DuetConversationReadRepository>();
        services.AddScoped<IDuetConversationWriteRepository, DuetConversationWriteRepository>();
        services.AddScoped<IUserProfileProjectionWriteRepository, UserProfileProjectionWriteRepository>();
        services.AddScoped<IUserProfileProjectionReadRepository, UserProfileProjectionReadRepository>();

        return services;
    }

    public static IServiceCollection AddWorkerPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EntityBaseSaveChangesInterceptor>();
        services.AddPostgresDbUpdateExceptionClassifier();
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("ChatDb"))
                .AddInterceptors(serviceProvider.GetRequiredService<EntityBaseSaveChangesInterceptor>()),
            ServiceLifetime.Scoped);

        return services;
    }
}

