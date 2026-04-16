using FlowChat.RealtimeService.Consumers.Configuration;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Routing.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class WorkerSettingsManagerTests
{
    [Fact]
    public void ConsumersSettingsManager_ResolvesConsumerSections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:ChatMessageSentConsumer:BootstrapServers"] = "broker:9092",
                ["Kafka:ChatMessageSentConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:ChatMessageSentConsumer:Topic"] = "chat-topic",
                ["Kafka:PresenceStatusChangedConsumer:BootstrapServers"] = "broker:9092",
                ["Kafka:PresenceStatusChangedConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:PresenceStatusChangedConsumer:Topic"] = "presence-topic",
                ["RealtimeApi:ApiKey"] = "worker-key",
                ["RealtimeApi:Instances:instance-a"] = "http://localhost:5215",
                ["RealtimeRouting:RedisConnectionString"] = "localhost:6379,password=secret",
                ["RealtimeRouting:KeyPrefix"] = "flowchat:test"
            })
            .Build();

        var settingsManager = new ConsumersSettingsManager(configuration);

        settingsManager.GetChatMessageSentConsumerOptions().BootstrapServers.Should().Be("broker:9092");
        settingsManager.GetChatMessageSentConsumerOptions().Topic.Should().Be("chat-topic");
        settingsManager.GetChatMessageSentConsumerOptions().RetryGroupId.Should().Be("realtime-service-retry");
        settingsManager.GetPresenceStatusChangedConsumerOptions().Topic.Should().Be("presence-topic");
        settingsManager.GetPresenceStatusChangedConsumerOptions().RetryGroupId.Should().Be("realtime-service-retry");
        settingsManager.GetRealtimeApiSettings().ApiKey.Should().Be("worker-key");
        settingsManager.GetRealtimeApiSettings().Instances.Should().ContainKey("instance-a")
            .WhoseValue.Should().Be("http://localhost:5215");
        settingsManager.GetRealtimeRoutingSettings().RedisConnectionString.Should().Be("localhost:6379,password=secret");
        settingsManager.GetRealtimeRoutingSettings().KeyPrefix.Should().Be("flowchat:test");
    }

    [Theory]
    [InlineData("RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.json")]
    [InlineData("RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredConsumerSections(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(RepositoryPathHelper.GetRepositoryPath(relativePath))
            .Build();

        var chatMessageOptions = configuration
            .GetSection(ChatMessageSentConsumerOptions.SectionName)
            .Get<ChatMessageSentConsumerOptions>();
        var presenceOptions = configuration
            .GetSection(PresenceStatusChangedConsumerOptions.SectionName)
            .Get<PresenceStatusChangedConsumerOptions>();
        var realtimeApiSettings = configuration
            .GetSection(RealtimeApiSettings.SectionName)
            .Get<RealtimeApiSettings>();
        var realtimeRoutingSettings = configuration
            .GetSection(RealtimeRoutingSettings.SectionName)
            .Get<RealtimeRoutingSettings>();

        chatMessageOptions.Should().NotBeNull();
        presenceOptions.Should().NotBeNull();
        realtimeApiSettings.Should().NotBeNull();
        realtimeRoutingSettings.Should().NotBeNull();
        chatMessageOptions!.Topic.Should().Be("dev.flowchat.chat.message.v1");
        chatMessageOptions.RetryGroupId.Should().Be("realtime-service-retry");
        presenceOptions!.Topic.Should().Be("dev.flowchat.presence.presence");
        presenceOptions.RetryGroupId.Should().Be("realtime-service-retry");
        realtimeApiSettings!.Instances.Should().ContainKey("flowchat-realtime-local");
        realtimeRoutingSettings!.KeyPrefix.Should().Be("flowchat:realtime");
    }
}
