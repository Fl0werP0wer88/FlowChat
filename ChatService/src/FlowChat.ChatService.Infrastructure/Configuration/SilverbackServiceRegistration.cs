using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.ChatService.Infrastructure.Kafka;

public static class ApiSilverbackServiceRegistration
{
    public static IServiceCollection AddApiSilverbackMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var conversationV2Options = configuration.GetSection(new ConversationV2ProducerSettingsSection().SectionName)
            .Get<ConversationV2ProducerSettingsSection>() ?? new ConversationV2ProducerSettingsSection();
        var membershipV2Options = configuration.GetSection(new ConversationMembershipV2ProjectionProducerSettingsSection().SectionName)
            .Get<ConversationMembershipV2ProjectionProducerSettingsSection>() ?? new ConversationMembershipV2ProjectionProducerSettingsSection();
        var participantV2Options = configuration.GetSection(new ConversationParticipantV2ProducerSettingsSection().SectionName)
            .Get<ConversationParticipantV2ProducerSettingsSection>() ?? new ConversationParticipantV2ProducerSettingsSection();
        var messageV2Options = configuration.GetSection(new ChatMessageV2ProducerSettingsSection().SectionName)
            .Get<ChatMessageV2ProducerSettingsSection>() ?? new ChatMessageV2ProducerSettingsSection();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(messageV2Options.BootstrapServers)
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<ConversationReadModelV2>>("conversation-v2-projection", endpoint => endpoint
                            .ProduceTo(conversationV2Options.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<DeltaProjectionIntegrationEventV2<ConversationMembershipReadModelV2>>("conversation-membership-v2-projection", endpoint => endpoint
                            .ProduceTo(membershipV2Options.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<ConversationParticipantReadModelV2>>("conversation-participant-v2-projection", endpoint => endpoint
                            .ProduceTo(participantV2Options.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())))
                    .AddProducer(producer => producer
                        .Produce<ChatMessageSentIntegrationEventV2>("chat-message-v2", endpoint => endpoint
                            .ProduceTo(messageV2Options.Topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())
                            .StoreToOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())));
            });

        return services;
    }
}
