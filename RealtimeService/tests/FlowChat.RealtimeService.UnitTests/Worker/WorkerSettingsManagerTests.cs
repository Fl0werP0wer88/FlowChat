using FlowChat.RealtimeService.Consumers.Configuration;
using FlowChat.RealtimeService.Consumers.Kafka;
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
                ["Kafka:ChatMessageSentConsumer:Topic"] = "chat-topic",
                ["Kafka:UserPresenceChangedConsumer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserPresenceChangedConsumer:Topic"] = "presence-topic",
                ["RealtimeApi:BaseUrl"] = "http://localhost:5215",
                ["RealtimeApi:ApiKey"] = "worker-key"
            })
            .Build();

        var settingsManager = new ConsumersSettingsManager(configuration);

        Assert.Equal("broker:9092", settingsManager.GetChatMessageSentConsumerOptions().BootstrapServers);
        Assert.Equal("chat-topic", settingsManager.GetChatMessageSentConsumerOptions().Topic);
        Assert.Equal("presence-topic", settingsManager.GetUserPresenceChangedConsumerOptions().Topic);
        Assert.Equal("http://localhost:5215", settingsManager.GetRealtimeApiSettings().BaseUrl);
        Assert.Equal("worker-key", settingsManager.GetRealtimeApiSettings().ApiKey);
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

        Assert.NotNull(chatMessageOptions);
        Assert.NotNull(presenceOptions);
        Assert.NotNull(realtimeApiSettings);
        Assert.Equal("dev.flowchat.chat.message.v1", chatMessageOptions!.Topic);
        Assert.Equal("dev.flowchat.user-profile.presence-changed.v1", presenceOptions!.Topic);
        Assert.Equal("http://localhost:5215", realtimeApiSettings!.BaseUrl);
    }
}
