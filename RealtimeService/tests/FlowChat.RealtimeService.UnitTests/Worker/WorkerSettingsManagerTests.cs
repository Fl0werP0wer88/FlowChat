using FlowChat.RealtimeService.Worker.Configuration;
using FlowChat.RealtimeService.Worker.Kafka;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class WorkerSettingsManagerTests
{
    [Fact]
    public void WorkerSettingsManager_ResolvesConsumerSections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:ChatMessageSentConsumer:BootstrapServers"] = "broker:9092",
                ["Kafka:ChatMessageSentConsumer:Topic"] = "chat-topic",
                ["Kafka:UserPresenceChangedConsumer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserPresenceChangedConsumer:Topic"] = "presence-topic"
            })
            .Build();

        var settingsManager = new WorkerSettingsManager(configuration);

        Assert.Equal("broker:9092", settingsManager.GetChatMessageSentConsumerOptions().BootstrapServers);
        Assert.Equal("chat-topic", settingsManager.GetChatMessageSentConsumerOptions().Topic);
        Assert.Equal("presence-topic", settingsManager.GetUserPresenceChangedConsumerOptions().Topic);
    }

    [Theory]
    [InlineData("RealtimeService/src/FlowChat.RealtimeService.Worker/appsettings.json")]
    [InlineData("RealtimeService/src/FlowChat.RealtimeService.Worker/appsettings.Development.json")]
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

        Assert.NotNull(chatMessageOptions);
        Assert.NotNull(presenceOptions);
        Assert.Equal("dev.flowchat.chat.message-sent.v1", chatMessageOptions!.Topic);
        Assert.Equal("dev.flowchat.user-profile.presence-changed.v1", presenceOptions!.Topic);
    }
}
