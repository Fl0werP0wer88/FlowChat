using FlowChat.RealtimeService.Consumers;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Application.Contracts.Persistence;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FlowChat.RealtimeService.Persistence;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
using FlowChat.RealtimeService.Redis.RealtimeConnections;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;
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
        var groupMembershipRepository = scope.ServiceProvider.GetRequiredService<IRealtimeGroupMembershipReadModelRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var conversationProjectionHandler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<RouteConversationProjectionV2Command, FlowChatResult<Unit>>>();
        var conversationMembershipDeltaHandler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<RouteConversationMembershipDeltaV2Command, FlowChatResult<Unit>>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var offsetCommitter = scope.ServiceProvider.GetRequiredService<IConsumedOffsetCommitter>();

        consumerCollection.Should().NotBeNull();
        chatSubscriber.Should().NotBeNull();
        presenceSubscriber.Should().NotBeNull();
        conversationSubscriber.Should().NotBeNull();
        conversationMembershipSubscriber.Should().NotBeNull();
        mediator.Should().NotBeNull();
        eventRouter.Should().BeOfType<WorkerRealtimeEventRouter>();
        routingReader.Should().NotBeNull();
        realtimeInstanceInternalApiClient.Should().NotBeNull();
        groupMembershipRepository.Should().NotBeNull();
        dbContext.Model.FindEntityType(typeof(SilverbackStoredOffset)).Should().NotBeNull();
        dbContext.Model.FindEntityType(typeof(SilverbackOutboxMessage)).Should().NotBeNull();
        dbContextFactory.Should().NotBeNull();
        conversationProjectionHandler.Should().NotBeNull();
        conversationMembershipDeltaHandler.Should().NotBeNull();
        unitOfWork.Should().BeSameAs(offsetCommitter)
            .And.BeOfType<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
    }

    [Fact]
    public async Task AddConsumers_RegistersKafkaConsumerInfrastructure()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddSingleton(Mock.Of<IConnectionMultiplexer>());
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var producerCollection = serviceProvider.GetRequiredService<IProducerCollection>();
        var topology = serviceProvider.GetRequiredService<TieredKafkaRetryTopology>();

        consumerCollection.Should().HaveCount(12);
        topology.Streams.SelectMany(stream => stream.RetryTiers.Select(tier => tier.Topic).Append(stream.DeadLetterTopic))
            .Should()
            .AllSatisfy(topic => producerCollection.GetProducerForEndpoint(topic).Should().NotBeNull());
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
        AssertRetryTiers(chatOptions, "dev.flowchat.chat.message.v2.realtime-service.retry");
        chatOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.message.v2.realtime-service.dlq");

        presenceOptions.Should().NotBeNull();
        presenceOptions!.GroupId.Should().Be("realtime-service");
        presenceOptions.RetryGroupId.Should().Be("realtime-service-retry");
        presenceOptions.Topic.Should().Be("dev.flowchat.presence.presence");
        AssertRetryTiers(presenceOptions, "dev.flowchat.presence.presence.realtime-service.retry");
        presenceOptions.DeadLetterTopic.Should().Be("dev.flowchat.presence.presence.realtime-service.dlq");

        conversationOptions.Should().NotBeNull();
        conversationOptions!.GroupId.Should().Be("realtime-service");
        conversationOptions.RetryGroupId.Should().Be("realtime-service-retry");
        conversationOptions.Topic.Should().Be("dev.flowchat.chat.conversation-projection.v2");
        AssertRetryTiers(conversationOptions, "dev.flowchat.chat.conversation-projection.v2.realtime-service.retry");
        conversationOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.conversation-projection.v2.realtime-service.dlq");

        conversationMembershipOptions.Should().NotBeNull();
        conversationMembershipOptions!.GroupId.Should().Be("realtime-service");
        conversationMembershipOptions.RetryGroupId.Should().Be("realtime-service-retry");
        conversationMembershipOptions.Topic.Should().Be("dev.flowchat.chat.conversation-membership-projection.v2");
        AssertRetryTiers(
            conversationMembershipOptions,
            "dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.retry");
        conversationMembershipOptions.DeadLetterTopic.Should().Be("dev.flowchat.chat.conversation-membership-projection.v2.realtime-service.dlq");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(
                "RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.Development.json"))
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = "internal-key",
                ["ConnectionStrings:Redis"] = "localhost:6379,password=secret",
                ["ConnectionStrings:RealtimeDb"] = "Host=localhost;Port=5432;Database=flowchat_realtime_db;Username=flowchat_app;Password=flowchat_app_pw;",
                ["RealtimeConnections:InstanceId"] = "realtime-consumers",
                ["RealtimeApi:Instances:realtime-api"] = "http://localhost:5215"
            })
            .Build();
    }

    private static void AssertRetryTiers(
        ITieredRetryKafkaConsumerSettingsSection settings,
        string firstRetryTopic)
    {
        settings.RetryTiers.Select(tier => tier.Topic).Should().Equal(
            $"{firstRetryTopic}.5s",
            $"{firstRetryTopic}.60s");
        settings.RetryTiers.Select(tier => tier.Delay).Should().Equal(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(60));
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
