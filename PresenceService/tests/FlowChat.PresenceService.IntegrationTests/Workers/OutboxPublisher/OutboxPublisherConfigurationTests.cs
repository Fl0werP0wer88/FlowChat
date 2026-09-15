using FlowChat.PresenceService.Consumers.Configuration.Settings;
using FlowChat.PresenceService.OutboxPublisher;
using FlowChat.PresenceService.OutboxPublisher.Configuration.Settings;
using FlowChat.PresenceService.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.PresenceService.IntegrationTests.Workers.OutboxPublisher;

public sealed class OutboxPublisherConfigurationTests
{
    [Fact]
    public async Task AddOutboxPublisher_RegistersBusinessAndRetryProducers()
    {
        var configuration = CreateOutboxConfiguration();
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

        topics.Should().HaveCount(3).And.OnlyHaveUniqueItems();
        topics.Should().AllSatisfy(topic =>
            producers.GetProducerForEndpoint(topic).Should().NotBeNull());
        producers.GetProducerForEndpoint("presence-status-changed").Should().NotBeNull();
    }

    [Fact]
    public void AppSettings_RetryOutboxTopicsMatchConsumerDestinations()
    {
        var outboxTopics = CreateOutboxConfiguration()
            .GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>()!
            .Topics;
        var consumerSettings = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(
                "PresenceService/src/Workers/FlowChat.PresenceService.Consumers/appsettings.json"))
            .Build()
            .GetSection(new ConversationParticipantV2ConsumerSettingsSection().SectionName)
            .Get<ConversationParticipantV2ConsumerSettingsSection>()!;

        outboxTopics.Should().BeEquivalentTo(
            consumerSettings.RetryTiers
                .Select(tier => tier.Topic)
                .Append(consumerSettings.DeadLetterTopic));
    }

    private static IConfiguration CreateOutboxConfiguration() => new ConfigurationBuilder()
        .AddJsonFile(GetRepositoryPath(
            "PresenceService/src/Workers/FlowChat.PresenceService.OutboxPublisher/appsettings.json"))
        .AddJsonFile(GetRepositoryPath(
            "PresenceService/src/Workers/FlowChat.PresenceService.OutboxPublisher/appsettings.Development.json"))
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
