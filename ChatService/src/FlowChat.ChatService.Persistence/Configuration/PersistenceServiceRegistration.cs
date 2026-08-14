using FlowChat.Shared.Application;
using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Persistence.Repositories;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace FlowChat.ChatService.Persistence;

public static class ApiPersistenceServiceRegistration
{
    public static IServiceCollection AddApiPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IUnitOfWork, SilverbackEfUnitOfWork<AppDbContext>>();
        services.AddCommonDbContextServices(configuration);
        services.AddChatRepositories();

        return services;
    }
}

public static class ConsumerPersistenceServiceRegistration
{
    public static IServiceCollection AddConsumerPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
        services.AddScoped<IUnitOfWork>(serviceProvider =>
            serviceProvider.GetRequiredService<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>());
        services.AddScoped<IConsumedOffsetCommitter>(serviceProvider =>
            serviceProvider.GetRequiredService<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>());
        services.AddCommonDbContextServices(configuration);
        services.AddChatRepositories();

        return services;
    }
}

public static class OutboxPublisherPersistenceServiceRegistration
{
    public static IServiceCollection AddOutboxPublisherPersistenceServices(this IServiceCollection services, IConfiguration configuration)
        => services.AddCommonDbContextServices(configuration);
}

internal static class CommonPersistenceServiceRegistration
{
    public static IServiceCollection AddCommonDbContextServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>((serviceProvider, options) =>
            options.UseNpgsql(configuration.GetConnectionString("ChatDb")));
        services.AddDbContextFactory<AppDbContext>(
            (serviceProvider, options) => options.UseNpgsql(configuration.GetConnectionString("ChatDb")),
            ServiceLifetime.Scoped);

        return services;
    }

    public static IServiceCollection AddChatRepositories(this IServiceCollection services)
    {
        services.AddScoped<IChatMessageReadRepository, ChatMessageReadRepository>();
        services.AddScoped<IChatMessageV2WriteRepository, ChatMessageV2WriteRepository>();
        services.AddScoped<IConversationParticipantReadRepository, ConversationParticipantReadRepository>();
        services.AddScoped<IConversationV2WriteRepository, ConversationV2WriteRepository>();
        services.AddScoped<IConversationMembershipWriteRepository, ConversationMembershipWriteRepository>();
        services.AddScoped<IConversationParticipantWriteRepository, ConversationParticipantWriteRepository>();
        services.AddScoped<IConversationMessageSequenceRepositoryV2, ConversationMessageSequenceRepositoryV2>();
        services.AddScoped<IConversationMessageSequenceReadRepository, ConversationMessageSequenceReadRepository>();
        services.AddScoped<IGroupConversationReadRepository, GroupConversationReadRepository>();
        services.AddScoped<IDuetConversationReadRepository, DuetConversationReadRepository>();
        services.AddScoped<IUserProfileProjectionReadRepository, UserProfileProjectionReadRepository>();

        return services;
    }
}

