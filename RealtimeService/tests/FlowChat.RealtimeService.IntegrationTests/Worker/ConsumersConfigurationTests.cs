using FlowChat.RealtimeService.Consumers;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FlowChat.RealtimeService.Persistence;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
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
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddSingleton(Mock.Of<IConnectionMultiplexer>());
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await using var scope = serviceProvider.CreateAsyncScope();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var chatSubscriber = scope.ServiceProvider.GetRequiredService<ChatMessageSentV2Subscriber>();
        var presenceSubscriber = scope.ServiceProvider.GetRequiredService<UserPresenceChangedSubscriber>();
        var conversationSubscriber = scope.ServiceProvider.GetRequiredService<ConversationProjectionV2Subscriber>();
        var conversationMembershipSubscriber = scope.ServiceProvider
            .GetRequiredService<ConversationMembershipDeltaV2Subscriber>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var eventRouter = scope.ServiceProvider.GetRequiredService<IRealtimeEventRouter>();
        var routingReader = scope.ServiceProvider.GetRequiredService<IUserInstanceRoutingReader>();
        var realtimeInstanceInternalApiClient = scope.ServiceProvider.GetRequiredService<IRealtimeInstanceInternalApiClient>();
        var chatServiceInternalApiClient = scope.ServiceProvider.GetRequiredService<IChatServiceInternalApiClient>();
        var groupMembershipRepository = scope.ServiceProvider.GetRequiredService<IRealtimeGroupMembershipReadModelRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var conversationProjectionHandler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<RouteConversationProjectionV2Command, FlowChatResult<Unit>>>();
        var conversationMembershipDeltaHandler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<RouteConversationMembershipDeltaV2Command, FlowChatResult<Unit>>>();

        consumerCollection.Should().NotBeNull();
        chatSubscriber.Should().NotBeNull();
        presenceSubscriber.Should().NotBeNull();
        conversationSubscriber.Should().NotBeNull();
        conversationMembershipSubscriber.Should().NotBeNull();
        mediator.Should().NotBeNull();
        eventRouter.Should().BeOfType<WorkerRealtimeEventRouter>();
        routingReader.Should().NotBeNull();
        realtimeInstanceInternalApiClient.Should().NotBeNull();
        chatServiceInternalApiClient.Should().NotBeNull();
        groupMembershipRepository.Should().NotBeNull();
        dbContext.Model.FindEntityType(typeof(SilverbackStoredOffset)).Should().NotBeNull();
        dbContextFactory.Should().NotBeNull();
        conversationProjectionHandler.Should().NotBeNull();
        conversationMembershipDeltaHandler.Should().NotBeNull();
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
            .GetSection(new ChatMessageV2ConsumerSettingsSection().SectionName)
            .Get<ChatMessageV2ConsumerSettingsSection>();
        var presenceOptions = configuration
            .GetSection(new PresenceStatusChangedConsumerSettingsSection().SectionName)
            .Get<PresenceStatusChangedConsumerSettingsSection>();
        var conversationOptions = configuration
            .GetSection(new ConversationV2ProjectionConsumerSettingsSection().SectionName)
            .Get<ConversationV2ProjectionConsumerSettingsSection>();
        var conversationMembershipOptions = configuration
            .GetSection(new ConversationMembershipV2ProjectionConsumerSettingsSection().SectionName)
            .Get<ConversationMembershipV2ProjectionConsumerSettingsSection>();

        chatOptions.Should().NotBeNull();
        chatOptions!.GroupId.Should().Be("realtime-service");
        chatOptions.RetryGroupId.Should().Be("realtime-service-retry");
        chatOptions.Topic.Should().Be("dev.flowchat.chat.message.v2");
        chatOptions.RetryTopic.Should().Be("dev.flowchat.chat.message.v2.realtime-service.retry");
        chatOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.message.v2.realtime-service.dlq");

        presenceOptions.Should().NotBeNull();
        presenceOptions!.GroupId.Should().Be("realtime-service");
        presenceOptions.RetryGroupId.Should().Be("realtime-service-retry");
        presenceOptions.Topic.Should().Be("dev.flowchat.presence.presence");
        presenceOptions.RetryTopic.Should().Be("dev.flowchat.presence.presence.realtime-service.retry");
        presenceOptions.DeadLetterTopic.Should().Be("dev.flowchat.presence.presence.realtime-service.dlq");

        conversationOptions.Should().NotBeNull();
        conversationOptions!.GroupId.Should().Be("realtime-service");
        conversationOptions.RetryGroupId.Should().Be("realtime-service-retry");
        conversationOptions.Topic.Should().Be("dev.flowchat.chat.conversation-projection.v2");
        conversationOptions.RetryTopic.Should().Be("dev.flowchat.chat.conversation-projection.v2.realtime-service.retry");
        conversationOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.conversation-projection.v2.realtime-service.dlq");

        conversationMembershipOptions.Should().NotBeNull();
        conversationMembershipOptions!.GroupId.Should().Be("realtime-service");
        conversationMembershipOptions.RetryGroupId.Should().Be("realtime-service-retry");
        conversationMembershipOptions.Topic.Should().Be("dev.flowchat.chat.conversation-membership-projection.v2");
        conversationMembershipOptions.RetryTopic.Should().Be("dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.retry");
        conversationMembershipOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.dlq");
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
                ["ConnectionStrings:RealtimeDb"] = "Host=localhost;Port=5432;Database=flowchat_realtime_db;Username=flowchat_app;Password=flowchat_app_pw;",
                ["RealtimeConnections:InstanceId"] = "realtime-consumers",
                ["RealtimeApi:Instances:realtime-api"] = "http://localhost:5215",
                ["ChatServiceApi:BaseUrl"] = "http://localhost:5254",
                ["ChatServiceApi:ApiKey"] = "internal-key",
                ["Kafka:ChatMessageV2Consumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:ChatMessageV2Consumer:GroupId"] = "realtime-service",
                ["Kafka:ChatMessageV2Consumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:ChatMessageV2Consumer:Topic"] = "dev.flowchat.chat.message.v2",
                ["Kafka:ChatMessageV2Consumer:RetryTopic"] = "dev.flowchat.chat.message.v2.retry",
                ["Kafka:ChatMessageV2Consumer:DeadLetterTopic"] = "dev.flowchat.chat.message.v2.dlq",
                ["Kafka:ChatMessageV2Consumer:MaxRetryCount"] = "5",
                ["Kafka:ChatMessageV2Consumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:ChatMessageV2Consumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:ChatMessageV2Consumer:AutoOffsetReset"] = "Earliest",
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
                ["Kafka:ConversationV2ProjectionConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:ConversationV2ProjectionConsumer:GroupId"] = "realtime-service",
                ["Kafka:ConversationV2ProjectionConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:ConversationV2ProjectionConsumer:Topic"] = "dev.flowchat.chat.conversation-projection.v2",
                ["Kafka:ConversationV2ProjectionConsumer:RetryTopic"] = "dev.flowchat.chat.conversation-projection.v2.retry",
                ["Kafka:ConversationV2ProjectionConsumer:DeadLetterTopic"] = "dev.flowchat.chat.conversation-projection.v2.dlq",
                ["Kafka:ConversationV2ProjectionConsumer:MaxRetryCount"] = "5",
                ["Kafka:ConversationV2ProjectionConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:ConversationV2ProjectionConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:ConversationV2ProjectionConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:GroupId"] = "realtime-service",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:Topic"] = "dev.flowchat.chat.conversation-membership-projection.v2",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:RetryTopic"] = "dev.flowchat.chat.conversation-membership-projection.v2.retry",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:DeadLetterTopic"] = "dev.flowchat.chat.conversation-membership-projection.v2.dlq",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:MaxRetryCount"] = "5",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:ConversationMembershipV2ProjectionConsumer:AutoOffsetReset"] = "Earliest"
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
