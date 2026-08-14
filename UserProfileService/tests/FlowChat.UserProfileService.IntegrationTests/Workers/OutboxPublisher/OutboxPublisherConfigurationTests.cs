using FlowChat.UserProfileService.Consumers.Configuration.Settings;
using FlowChat.UserProfileService.OutboxPublisher;
using FlowChat.UserProfileService.OutboxPublisher.Configuration.Settings;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.UserProfileService.IntegrationTests.Workers.OutboxPublisher;

public sealed class OutboxPublisherConfigurationTests
{
    [Fact]
    public async Task AddOutboxPublisher_RegistersProducerForEveryRetryAndDlqEndpoint()
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
        topics.Should().AllSatisfy(topic => producers.GetProducerForEndpoint(topic).Should().NotBeNull());
    }

    [Fact]
    public void AppSettings_OutboxTopicsMatchConsumerRetryAndDlqDestinations()
    {
        var outboxTopics = CreateOutboxConfiguration()
            .GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>()!
            .Topics;
        var consumerConfiguration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(
                "UserProfileService/src/Workers/FlowChat.UserProfileService.Consumers/appsettings.json"))
            .Build();
        var consumerSettings = consumerConfiguration
            .GetSection(new AccountRegisteredConsumerSettingsSection().SectionName)
            .Get<AccountRegisteredConsumerSettingsSection>()!;
        var destinations = consumerSettings.RetryTiers
            .Select(tier => tier.Topic)
            .Append(consumerSettings.DeadLetterTopic);

        outboxTopics.Should().BeEquivalentTo(destinations);
    }

    private static IConfiguration CreateOutboxConfiguration() => new ConfigurationBuilder()
        .AddJsonFile(GetRepositoryPath(
            "UserProfileService/src/Workers/FlowChat.UserProfileService.OutboxPublisher/appsettings.json"))
        .AddJsonFile(GetRepositoryPath(
            "UserProfileService/src/Workers/FlowChat.UserProfileService.OutboxPublisher/appsettings.Development.json"))
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
