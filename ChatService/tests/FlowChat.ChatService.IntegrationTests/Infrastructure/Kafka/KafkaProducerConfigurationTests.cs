using FlowChat.ChatService.Infrastructure;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.ChatService.IntegrationTests.Infrastructure.Kafka;

public sealed class KafkaProducerConfigurationTests
{
    [Fact]
    public void AddApiInfrastructureServices_RegistersOnlyV2ChatProducerTypes()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddApiInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<KafkaProducerSettingsRegistry>();

        registry.Get<ProjectionIntegrationEvent<ConversationReadModelV2>>()!.Topic
            .Should().Be("dev.flowchat.chat.conversation-projection.v2");
        registry.Get<DeltaProjectionIntegrationEventV2<ConversationMembershipReadModelV2>>()!.Topic
            .Should().Be("dev.flowchat.chat.conversation-membership-projection.v2");
        registry.Get<ProjectionIntegrationEvent<ConversationParticipantReadModelV2>>()!.Topic
            .Should().Be("dev.flowchat.chat.conversation-participant-projection.v2");
        registry.Get<ChatMessageSentIntegrationEventV2>()!.Topic
            .Should().Be("dev.flowchat.chat.message.v2");

        registry.Get<ChatMessageSentIntegrationEvent>().Should().BeNull();
        registry.Get<GroupConversationChangedIntegrationEvent>().Should().BeNull();
        registry.Get<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>().Should().BeNull();
        registry.Get<ProjectionIntegrationEvent<DuetConversationMembershipReadModel>>().Should().BeNull();
        registry.Get<ProjectionIntegrationEvent<DuetConversationContactStateReadModel>>().Should().BeNull();
    }
}
