using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.OutboxPublisher;
using FlowChat.RealtimeService.OutboxPublisher.Configuration.Settings;
using FlowChat.RealtimeService.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;

namespace FlowChat.RealtimeService.IntegrationTests.Worker;

public sealed class OutboxPublisherConfigurationTests
{
    [Fact]
    public async Task AddOutboxPublisher_RegistersProducerForEveryRetryAndDlqEndpoint()
    {
        var configuration = CreateConfiguration();
        var topics = configuration
            .GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>()!
            .Topics;
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddOutboxPublisherPersistenceServices(configuration);
        services.AddOutboxPublisher(configuration);

        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
        await provider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        var producers = provider.GetRequiredService<IProducerCollection>();

        topics.Should().HaveCount(20).And.OnlyHaveUniqueItems();
        topics.Should().AllSatisfy(topic => producers.GetProducerForEndpoint(topic).Should().NotBeNull());
    }

    [Fact]
    public void AppSettings_OutboxTopicsMatchConsumerRetryAndDlqDestinations()
    {
        var outboxConfiguration = CreateConfiguration();
        var consumersConfiguration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(
                "RealtimeService/src/Workers/FlowChat.RealtimeService.Consumers/appsettings.json"))
            .Build();

        var outboxTopics = outboxConfiguration
            .GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>()!
            .Topics;
        ITieredRetryKafkaConsumerSettingsSection[] streams =
        [
            consumersConfiguration.GetSection(new ChatMessageV2ConsumerSettingsSection().SectionName)
                .Get<ChatMessageV2ConsumerSettingsSection>()!,
            consumersConfiguration.GetSection(new PresenceStatusChangedConsumerSettingsSection().SectionName)
                .Get<PresenceStatusChangedConsumerSettingsSection>()!,
            consumersConfiguration.GetSection(new ConversationV2ProjectionConsumerSettingsSection().SectionName)
                .Get<ConversationV2ProjectionConsumerSettingsSection>()!,
            consumersConfiguration.GetSection(new ConversationMembershipV2ProjectionConsumerSettingsSection().SectionName)
                .Get<ConversationMembershipV2ProjectionConsumerSettingsSection>()!
        ];
        var consumerDestinations = streams
            .SelectMany(stream => stream.RetryTiers.Select(tier => tier.Topic).Append(stream.DeadLetterTopic));

        outboxTopics.Should().BeEquivalentTo(consumerDestinations);
    }

    private static IConfiguration CreateConfiguration() => new ConfigurationBuilder()
        .AddJsonFile(GetRepositoryPath(
            "RealtimeService/src/Workers/FlowChat.RealtimeService.OutboxPublisher/appsettings.json"))
        .AddJsonFile(GetRepositoryPath(
            "RealtimeService/src/Workers/FlowChat.RealtimeService.OutboxPublisher/appsettings.Development.json"))
        .Build();

    private static string GetRepositoryPath(string relativePath)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, relativePath);
            if (File.Exists(candidatePath))
                return candidatePath;

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}'.");
    }
}
