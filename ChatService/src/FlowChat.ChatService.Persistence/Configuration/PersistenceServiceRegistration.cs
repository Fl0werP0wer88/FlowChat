using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;
using FlowChat.ChatService.Application.Features.Conversation.Commands.AddParticipant;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;
using FlowChat.ChatService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence.BulkUpsert;
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
        services.AddPostgresDbUpdateExceptionClassifier(options =>
        {
            options.UniqueConstraintNamesByIdempotencyConflictKey[SendChatMessageCommand.IdempotencyConflictKey] =
                ["PK_ChatMessages"];
            options.UniqueConstraintNamesByIdempotencyConflictKey[InsertUserProfileProjectionCommand.IdempotencyConflictKey] =
                ["PK_UserProfileProjections"];
            options.UniqueConstraintNamesByIdempotencyConflictKey[CreateGroupConversationCommand.IdempotencyConflictKey] =
                ["PK_Conversations"];
            options.UniqueConstraintNamesByIdempotencyConflictKey[CreateGroupFromDuetCommand.IdempotencyConflictKey] =
                ["PK_Conversations"];
            options.UniqueConstraintNamesByIdempotencyConflictKey[CreateDuetConversationCommand.IdempotencyConflictKey] =
                ["PK_DuetConversations"];
            options.UniqueConstraintNamesByIdempotencyConflictKey[AddParticipantCommand.IdempotencyConflictKey] =
                ["IX_ParticipantUsers_ConversationId_UserId"];
        });
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
        services.AddScoped<IGroupConversationReadRepository, GroupConversationReadRepository>();
        services.AddScoped<IDuetConversationReadRepository, DuetConversationReadRepository>();
        services.AddScoped<IDuetConversationWriteRepository, DuetConversationWriteRepository>();
        services.AddScoped<IUserProfileProjectionWriteRepository, UserProfileProjectionWriteRepository>();
        services.AddScoped<IUserProfileProjectionReadRepository, UserProfileProjectionReadRepository>();
        services.AddScoped<IBulkUpsertExecutor<UserProfileProjectionDto>, UserProfileProjectionBulkUpsertExecutor>();

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

