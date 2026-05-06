using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Redis.Configuration.Settings;
using FlowChat.Shared.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class WorkerSettingsManagerTests
{
    [Fact]
    public void SettingsProvider_ResolvesConsumerSections()
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

        var settingsProvider = new AppSettingsProvider(configuration);

        settingsProvider.GetSection<ChatMessageSentConsumerSettingsSection>().BootstrapServers.Should().Be("broker:9092");
        settingsProvider.GetSection<ChatMessageSentConsumerSettingsSection>().Topic.Should().Be("chat-topic");
        settingsProvider.GetSection<ChatMessageSentConsumerSettingsSection>().RetryGroupId.Should().Be("realtime-service-retry");
        settingsProvider.GetSection<PresenceStatusChangedConsumerSettingsSection>().Topic.Should().Be("presence-topic");
        settingsProvider.GetSection<PresenceStatusChangedConsumerSettingsSection>().RetryGroupId.Should().Be("realtime-service-retry");
        settingsProvider.GetSection<RealtimeApiSettingsSection>().ApiKey.Should().Be("worker-key");
        settingsProvider.GetSection<RealtimeApiSettingsSection>().Instances.Should().ContainKey("instance-a")
            .WhoseValue.Should().Be("http://localhost:5215");
        settingsProvider.GetSection<RealtimeRoutingSettingsSection>().RedisConnectionString.Should().Be("localhost:6379,password=secret");
        settingsProvider.GetSection<RealtimeRoutingSettingsSection>().KeyPrefix.Should().Be("flowchat:test");
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
            .GetSection(new ChatMessageSentConsumerSettingsSection().SectionName)
            .Get<ChatMessageSentConsumerSettingsSection>();
        var presenceOptions = configuration
            .GetSection(new PresenceStatusChangedConsumerSettingsSection().SectionName)
            .Get<PresenceStatusChangedConsumerSettingsSection>();
        var realtimeApiSettings = configuration
            .GetSection(new RealtimeApiSettingsSection().SectionName)
            .Get<RealtimeApiSettingsSection>();
        var realtimeRoutingSettings = configuration
            .GetSection(new RealtimeRoutingSettingsSection().SectionName)
            .Get<RealtimeRoutingSettingsSection>();

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
