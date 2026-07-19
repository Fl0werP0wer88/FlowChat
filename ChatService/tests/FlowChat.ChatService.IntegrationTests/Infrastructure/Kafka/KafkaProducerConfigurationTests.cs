using FlowChat.ChatService.Infrastructure;
using FlowChat.ChatService.Application.Contracts.Messaging;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.ChatService.IntegrationTests.Infrastructure.Kafka;

public sealed class KafkaProducerConfigurationTests
{
    [Fact]
    public void AddApiInfrastructureServices_RegistersBothDuetProjectionTypesWithSharedTopic()
    {
        const string projectionTopic = "dev.flowchat.chat.duet-conversation-projection.v1";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:DuetConversationProjectionProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:DuetConversationProjectionProducer:Topic"] = projectionTopic
            })
            .Build();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddApiInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var registry = serviceProvider.GetRequiredService<KafkaProducerSettingsRegistry>();
        var membershipSettings = registry.Get<ProjectionIntegrationEvent<DuetConversationMembershipReadModel>>();
        var contactStateSettings = registry.Get<ProjectionIntegrationEvent<DuetConversationContactStateReadModel>>();

        membershipSettings.Should().NotBeNull();
        contactStateSettings.Should().NotBeNull();
        membershipSettings!.Topic.Should().Be(projectionTopic);
        contactStateSettings!.Topic.Should().Be(projectionTopic);
    }

    [Fact]
    public void AddApiInfrastructureServices_RegistersAllV2TypesOnIsolatedTopics()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddApiInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();
        var registry = serviceProvider.GetRequiredService<KafkaProducerSettingsRegistry>();

        registry.Get<ProjectionIntegrationEvent<ConversationReadModelV2>>()!.Topic
            .Should().Be("dev.flowchat.chat.conversation-v2-projection.v1");
        registry.Get<DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>>()!.Topic
            .Should().Be("dev.flowchat.chat.conversation-membership-v2-projection.v1");
        registry.Get<ProjectionIntegrationEvent<ConversationParticipantReadModelV2>>()!.Topic
            .Should().Be("dev.flowchat.chat.conversation-participant-v2-projection.v1");
        registry.Get<ChatMessageSentIntegrationEventV2>()!.Topic
            .Should().Be("dev.flowchat.chat.message-v2.v1");
    }
}
