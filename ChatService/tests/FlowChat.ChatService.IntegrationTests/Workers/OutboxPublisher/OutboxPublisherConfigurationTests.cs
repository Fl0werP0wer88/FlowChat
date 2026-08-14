using FlowChat.ChatService.Consumers.Configuration.Settings;
using FlowChat.ChatService.OutboxPublisher;
using FlowChat.ChatService.OutboxPublisher.Configuration.Settings;
using FlowChat.ChatService.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.ChatService.IntegrationTests.Workers.OutboxPublisher;

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

        topics.Should().HaveCount(5).And.OnlyHaveUniqueItems();
        topics.Should().AllSatisfy(topic =>
            producers.GetProducerForEndpoint(topic).Should().NotBeNull());
        producers.GetProducerForEndpoint("conversation-v2-projection").Should().NotBeNull();
        producers.GetProducerForEndpoint("conversation-membership-v2-projection").Should().NotBeNull();
        producers.GetProducerForEndpoint("conversation-participant-v2-projection").Should().NotBeNull();
        producers.GetProducerForEndpoint("chat-message-v2").Should().NotBeNull();
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
                "ChatService/src/Workers/FlowChat.ChatService.Consumers/appsettings.json"))
            .Build()
            .GetSection(new UserProfileConsumerSettingsSection().SectionName)
            .Get<UserProfileConsumerSettingsSection>()!;

        outboxTopics.Should().BeEquivalentTo(
            consumerSettings.RetryTiers
                .Select(tier => tier.Topic)
                .Append(consumerSettings.DeadLetterTopic));
    }

    private static IConfiguration CreateOutboxConfiguration() => new ConfigurationBuilder()
        .AddJsonFile(GetRepositoryPath(
            "ChatService/src/Workers/FlowChat.ChatService.OutboxPublisher/appsettings.json"))
        .AddJsonFile(GetRepositoryPath(
            "ChatService/src/Workers/FlowChat.ChatService.OutboxPublisher/appsettings.Development.json"))
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
