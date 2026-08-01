using FlowChat.AuthService.Consumers.Configuration.Settings;
using FlowChat.AuthService.OutboxPublisher;
using FlowChat.AuthService.OutboxPublisher.Configuration.Settings;
using FlowChat.AuthService.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Publishing;

namespace FlowChat.AuthService.IntegrationTests.OutboxPublisher.Configuration;

public sealed class OutboxPublisherConfigurationTests
{
    [Fact]
    public async Task AddOutboxPublisher_RegistersBusinessAndRetryProducers()
    {
        var configuration = CreateOutboxConfiguration();
        var retryTopics = configuration
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

        retryTopics.Should().HaveCount(5).And.OnlyHaveUniqueItems();
        retryTopics.Should().AllSatisfy(topic =>
            producers.GetProducerForEndpoint(topic).Should().NotBeNull());
        producers.GetProducerForEndpoint("auth-account-registered").Should().NotBeNull();
        producers.GetProducerForEndpoint("auth-account-confirmed").Should().NotBeNull();
        producers.GetProducerForEndpoint("auth-user-phone-confirmed").Should().NotBeNull();
    }

    [Fact]
    public void AppSettings_RetryOutboxTopicsMatchConsumerDestinations()
    {
        var outboxTopics = CreateOutboxConfiguration()
            .GetSection(new RetryOutboxKafkaSettingsSection().SectionName)
            .Get<RetryOutboxKafkaSettingsSection>()!
            .Topics;
        var consumerConfiguration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(
                "AuthService/src/Workers/FlowChat.AuthService.Consumers/appsettings.json"))
            .Build();
        var consumerSettings = consumerConfiguration
            .GetSection(new UserEmailConfirmedConsumerSettingsSection().SectionName)
            .Get<UserEmailConfirmedConsumerSettingsSection>()!;
        var destinations = consumerSettings.RetryTiers
            .Select(tier => tier.Topic)
            .Append(consumerSettings.DeadLetterTopic);

        outboxTopics.Should().BeEquivalentTo(destinations);
    }

    private static IConfiguration CreateOutboxConfiguration() => new ConfigurationBuilder()
        .AddJsonFile(GetRepositoryPath(
            "AuthService/src/Workers/FlowChat.AuthService.OutboxPublisher/appsettings.json"))
        .AddJsonFile(GetRepositoryPath(
            "AuthService/src/Workers/FlowChat.AuthService.OutboxPublisher/appsettings.Development.json"))
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
