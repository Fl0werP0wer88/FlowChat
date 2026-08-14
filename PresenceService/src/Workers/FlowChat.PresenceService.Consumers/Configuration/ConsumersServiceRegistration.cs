using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.PresenceService.Application;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Consumers.Configuration.Settings;
using FlowChat.PresenceService.Consumers.Kafka.Projections;
using FlowChat.PresenceService.Infrastructure;
using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.Projections;
using FlowChat.Shared.Consumers.Projections.Single;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.PresenceService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var contactOptions = configuration
            .GetSection(new ConversationParticipantV2ConsumerSettingsSection().SectionName)
            .Get<ConversationParticipantV2ConsumerSettingsSection>()
            ?? new ConversationParticipantV2ConsumerSettingsSection();

        services.AddConsumerApplicationServices();
        services.AddConsumerPersistenceServices(configuration);
        services.AddConsumerInfrastructureServices(configuration);

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddFlowChatTieredRetryConsumerPipeline<AppDbContext>(
                contactOptions.BootstrapServers,
                [contactOptions])
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkOutbox())
            .AddProjectionSingle<
                ConversationParticipantReadModelV2,
                ContactObserverProjectionDto,
                ContactObserverProjectionRepository,
                ContactObserverProjectionSubscriber>();

        return services;
    }
}
