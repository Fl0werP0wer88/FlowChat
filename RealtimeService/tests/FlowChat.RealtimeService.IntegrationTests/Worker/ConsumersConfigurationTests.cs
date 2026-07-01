using FlowChat.RealtimeService.Consumers;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConsumersConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersConsumerInfrastructure()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IConnectionMultiplexer>());
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var chatSubscriber = scope.ServiceProvider.GetRequiredService<ChatMessageSentSubscriber>();
        var presenceSubscriber = scope.ServiceProvider.GetRequiredService<UserPresenceChangedSubscriber>();
        var conversationSubscriber = scope.ServiceProvider.GetRequiredService<GroupConversationChangedSubscriber>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var eventRouter = scope.ServiceProvider.GetRequiredService<IRealtimeEventRouter>();
        var routingReader = scope.ServiceProvider.GetRequiredService<IUserInstanceRoutingReader>();
        var realtimeInstanceInternalApiClient = scope.ServiceProvider.GetRequiredService<IRealtimeInstanceInternalApiClient>();
        var chatServiceInternalApiClient = scope.ServiceProvider.GetRequiredService<IChatServiceInternalApiClient>();

        consumerCollection.Should().NotBeNull();
        chatSubscriber.Should().NotBeNull();
        presenceSubscriber.Should().NotBeNull();
        conversationSubscriber.Should().NotBeNull();
        mediator.Should().NotBeNull();
        eventRouter.Should().BeOfType<WorkerRealtimeEventRouter>();
        routingReader.Should().NotBeNull();
        realtimeInstanceInternalApiClient.Should().NotBeNull();
        chatServiceInternalApiClient.Should().NotBeNull();
    }

    [Fact]
    public async Task AddConsumers_RegistersKafkaConsumerInfrastructure()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IConnectionMultiplexer>());
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();

        consumerCollection.Should().NotBeNull();
    }

    [Theory]
    [InlineData("RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.json")]
    [InlineData("RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSections(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var chatOptions = configuration
            .GetSection(new ChatMessageSentConsumerSettingsSection().SectionName)
            .Get<ChatMessageSentConsumerSettingsSection>();
        var presenceOptions = configuration
            .GetSection(new PresenceStatusChangedConsumerSettingsSection().SectionName)
            .Get<PresenceStatusChangedConsumerSettingsSection>();
        var conversationOptions = configuration
            .GetSection(new GroupConversationChangedConsumerSettingsSection().SectionName)
            .Get<GroupConversationChangedConsumerSettingsSection>();

        chatOptions.Should().NotBeNull();
        chatOptions!.GroupId.Should().Be("realtime-service");
        chatOptions.RetryGroupId.Should().Be("realtime-service-retry");
        chatOptions.Topic.Should().Be("dev.flowchat.chat.message.v1");
        chatOptions.RetryTopic.Should().Be("dev.flowchat.chat.message.v1.realtime-service.retry");
        chatOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.message.v1.realtime-service.dlq");

        presenceOptions.Should().NotBeNull();
        presenceOptions!.GroupId.Should().Be("realtime-service");
        presenceOptions.RetryGroupId.Should().Be("realtime-service-retry");
        presenceOptions.Topic.Should().Be("dev.flowchat.presence.presence");
        presenceOptions.RetryTopic.Should().Be("dev.flowchat.presence.presence.realtime-service.retry");
        presenceOptions.DeadLetterTopic.Should().Be("dev.flowchat.presence.presence.realtime-service.dlq");

        conversationOptions.Should().NotBeNull();
        conversationOptions!.GroupId.Should().Be("realtime-service");
        conversationOptions.RetryGroupId.Should().Be("realtime-service-retry");
        conversationOptions.Topic.Should().Be("dev.flowchat.chat.group-conversation.v1");
        conversationOptions.RetryTopic.Should().Be("dev.flowchat.chat.group-conversation.v1.realtime-service.retry");
        conversationOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.group-conversation.v1.realtime-service.dlq");
    }

    [Fact]
    public void DevelopmentAppSettings_UseChatServiceInternalApiKey()
    {
        var realtimeConsumersConfiguration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath("RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.json"))
            .AddJsonFile(GetRepositoryPath("RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.Development.json"))
            .Build();
        var chatServiceApiConfiguration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath("ChatService/src/FlowChat.ChatService.API/appsettings.json"))
            .AddJsonFile(GetRepositoryPath("ChatService/src/FlowChat.ChatService.API/appsettings.Development.json"))
            .Build();

        var realtimeChatServiceOptions = realtimeConsumersConfiguration
            .GetSection(new ChatServiceSettingsSection().SectionName)
            .Get<ChatServiceSettingsSection>();
        var chatServiceInternalApiKey = chatServiceApiConfiguration["FlowChat:InternalApi:ApiKey"];

        realtimeChatServiceOptions.Should().NotBeNull();
        realtimeChatServiceOptions!.ApiKey.Should().Be(chatServiceInternalApiKey);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ConnectionStrings:Redis"] = "localhost:6379,password=secret",
                ["RealtimeConnections:InstanceId"] = "realtime-consumers",
                ["RealtimeApi:Instances:realtime-api"] = "http://localhost:5215",
                ["ChatServiceApi:BaseUrl"] = "http://localhost:5254",
                ["ChatServiceApi:ApiKey"] = "internal-key",
                ["Kafka:ChatMessageSentConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:ChatMessageSentConsumer:GroupId"] = "realtime-service",
                ["Kafka:ChatMessageSentConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:ChatMessageSentConsumer:Topic"] = "dev.flowchat.chat.message.v1",
                ["Kafka:ChatMessageSentConsumer:RetryTopic"] = "dev.flowchat.chat.message.v1.retry",
                ["Kafka:ChatMessageSentConsumer:DeadLetterTopic"] = "dev.flowchat.chat.message.v1.dlq",
                ["Kafka:ChatMessageSentConsumer:MaxRetryCount"] = "5",
                ["Kafka:ChatMessageSentConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:ChatMessageSentConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:ChatMessageSentConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:PresenceStatusChangedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:PresenceStatusChangedConsumer:GroupId"] = "realtime-service",
                ["Kafka:PresenceStatusChangedConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:PresenceStatusChangedConsumer:Topic"] = "dev.flowchat.presence.presence",
                ["Kafka:PresenceStatusChangedConsumer:RetryTopic"] = "dev.flowchat.presence.presence.retry",
                ["Kafka:PresenceStatusChangedConsumer:DeadLetterTopic"] = "dev.flowchat.presence.presence.dlq",
                ["Kafka:PresenceStatusChangedConsumer:MaxRetryCount"] = "5",
                ["Kafka:PresenceStatusChangedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:PresenceStatusChangedConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:PresenceStatusChangedConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:GroupConversationChangedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:GroupConversationChangedConsumer:GroupId"] = "realtime-service",
                ["Kafka:GroupConversationChangedConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:GroupConversationChangedConsumer:Topic"] = "dev.flowchat.chat.group-conversation.v1",
                ["Kafka:GroupConversationChangedConsumer:RetryTopic"] = "dev.flowchat.chat.group-conversation.v1.retry",
                ["Kafka:GroupConversationChangedConsumer:DeadLetterTopic"] = "dev.flowchat.chat.group-conversation.v1.dlq",
                ["Kafka:GroupConversationChangedConsumer:MaxRetryCount"] = "5",
                ["Kafka:GroupConversationChangedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:GroupConversationChangedConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:GroupConversationChangedConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();
    }

    private static string GetRepositoryPath(string relativePath)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, relativePath);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}' starting from '{AppContext.BaseDirectory}'.");
    }
}
