using FlowChat.RealtimeService.Consumers.Configuration;
using FlowChat.RealtimeService.Consumers.Kafka;
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
                ["Kafka:UserPresenceChangedConsumer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserPresenceChangedConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:UserPresenceChangedConsumer:Topic"] = "presence-topic",
                ["RealtimeApi:BaseUrl"] = "http://localhost:5215",
                ["RealtimeApi:ApiKey"] = "worker-key"
            })
            .Build();

        var settingsManager = new ConsumersSettingsManager(configuration);

        settingsManager.GetChatMessageSentConsumerOptions().BootstrapServers.Should().Be("broker:9092");
        settingsManager.GetChatMessageSentConsumerOptions().Topic.Should().Be("chat-topic");
        settingsManager.GetChatMessageSentConsumerOptions().RetryGroupId.Should().Be("realtime-service-retry");
        settingsManager.GetUserPresenceChangedConsumerOptions().Topic.Should().Be("presence-topic");
        settingsManager.GetUserPresenceChangedConsumerOptions().RetryGroupId.Should().Be("realtime-service-retry");
        settingsManager.GetRealtimeApiSettings().BaseUrl.Should().Be("http://localhost:5215");
        settingsManager.GetRealtimeApiSettings().ApiKey.Should().Be("worker-key");
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
            .GetSection(UserPresenceChangedConsumerOptions.SectionName)
            .Get<UserPresenceChangedConsumerOptions>();
        var realtimeApiSettings = configuration
            .GetSection(RealtimeApiSettings.SectionName)
            .Get<RealtimeApiSettings>();

        chatMessageOptions.Should().NotBeNull();
        presenceOptions.Should().NotBeNull();
        realtimeApiSettings.Should().NotBeNull();
        chatMessageOptions!.Topic.Should().Be("dev.flowchat.chat.message.v1");
        chatMessageOptions.RetryGroupId.Should().Be("realtime-service-retry");
        presenceOptions!.Topic.Should().Be("dev.flowchat.user-profile.presence-changed.v1");
        presenceOptions.RetryGroupId.Should().Be("realtime-service-retry");
        realtimeApiSettings!.BaseUrl.Should().Be("http://localhost:5215");
    }
}
