using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Consumers;
using FlowChat.PresenceService.Consumers.Configuration.Settings;
using FlowChat.PresenceService.Consumers.Kafka.Projections;
using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.Projections;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.PresenceService.IntegrationTests.Workers.Consumers.Kafka;

public sealed class ConversationParticipantV2ConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersSingleProjectionTieredRetryPipeline()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddConsumers(configuration);

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await provider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        await using var scope = provider.CreateAsyncScope();

        var consumers = provider.GetRequiredService<IConsumerCollection>();
        var producers = provider.GetRequiredService<IProducerCollection>();
        var topology = provider.GetRequiredService<TieredKafkaRetryTopology>();
        var repository = scope.ServiceProvider
            .GetRequiredService<IProjectionSingleRepository<ContactObserverProjectionDto>>();
        var handler = scope.ServiceProvider.GetRequiredService<
            IRequestHandler<ProjectionSingleCommand<ContactObserverProjectionDto>, FlowChatResult<Unit>>>();
        var subscriber = scope.ServiceProvider.GetRequiredService<ContactObserverProjectionSubscriber>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var offsetCommitter = scope.ServiceProvider.GetRequiredService<IConsumedOffsetCommitter>();

        consumers.Should().HaveCount(5);
        topology.Streams.Should().ContainSingle();
        topology.Streams[0].RetryTiers
            .Select(tier => tier.Topic)
            .Append(topology.Streams[0].DeadLetterTopic)
            .Should()
            .AllSatisfy(topic => producers.GetProducerForEndpoint(topic).Should().NotBeNull());
        repository.Should().BeOfType<ContactObserverProjectionRepository>();
        handler.Should().BeOfType<ProjectionSingleCommandHandler<ContactObserverProjectionDto>>();
        subscriber.Should().NotBeNull();
        unitOfWork.Should().BeSameAs(offsetCommitter)
            .And.BeOfType<SilverbackKafkaOffsetUnitOfWork<AppDbContext>>();
    }

    [Theory]
    [InlineData("PresenceService/src/Workers/FlowChat.PresenceService.Consumers/appsettings.json")]
    [InlineData("PresenceService/src/Workers/FlowChat.PresenceService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeTieredRetrySettings(string relativePath)
    {
        var settings = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build()
            .GetSection(new ConversationParticipantV2ConsumerSettingsSection().SectionName)
            .Get<ConversationParticipantV2ConsumerSettingsSection>();

        settings.Should().NotBeNull();
        settings!.GroupId.Should().Be("presence-service");
        settings.RetryGroupId.Should().Be("presence-service-conversation-participant-v2-retry");
        settings.Topic.Should().Be("dev.flowchat.chat.conversation-participant-projection.v2");
        settings.RetryTiers.Select(tier => tier.Topic).Should().Equal(
            "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.5s",
            "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.20s",
            "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.60s",
            "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.300s");
        settings.RetryTiers.Select(tier => tier.Delay).Should().Equal(
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(300));
        settings.DeadLetterTopic.Should().Be(
            "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.dlq");
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PresenceDb"] = "Host=localhost;Database=presence-test",
            ["Kafka:ConversationParticipantV2Consumer:BootstrapServers"] = "localhost:9092",
            ["Kafka:ConversationParticipantV2Consumer:GroupId"] = "presence-service",
            ["Kafka:ConversationParticipantV2Consumer:RetryGroupId"] = "presence-service-conversation-participant-v2-retry",
            ["Kafka:ConversationParticipantV2Consumer:Topic"] = "dev.flowchat.chat.conversation-participant-projection.v2",
            ["Kafka:ConversationParticipantV2Consumer:DeadLetterTopic"] = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.dlq",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:0:Topic"] = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.5s",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:0:Delay"] = "00:00:05",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:1:Topic"] = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.20s",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:1:Delay"] = "00:00:20",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:2:Topic"] = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.60s",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:2:Delay"] = "00:01:00",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:3:Topic"] = "dev.flowchat.chat.conversation-participant-projection.v2.presence-service.retry.300s",
            ["Kafka:ConversationParticipantV2Consumer:RetryTiers:3:Delay"] = "00:05:00",
            ["Kafka:ConversationParticipantV2Consumer:AutoOffsetReset"] = "Earliest"
        })
        .Build();

    private static string GetRepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}'.");
    }
}
