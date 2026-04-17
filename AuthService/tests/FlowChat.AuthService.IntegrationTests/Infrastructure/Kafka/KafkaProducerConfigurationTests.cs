using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.UnitTests;

public sealed class KafkaProducerConfigurationTests
{
    [Fact]
    public void AddInfrastructureServices_ResolvesKafkaProducerOptions_WithoutFallbackToLegacySections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:AccountRegisteredConsumer:BootstrapServers"] = "legacy-broker:9092",
                ["Kafka:AccountRegisteredConsumer:Topic"] = "legacy-user-created-topic",
                ["Kafka:AccountRegisteredProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:AccountRegisteredProducer:Topic"] = "user-created-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var settingsManager = serviceProvider.GetRequiredService<IWorkerSettingsManager>();
        var accountRegisteredOptions = settingsManager.GetAccountRegisteredProducerSettingsSection();
        var typedAccountRegisteredOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<AccountRegisteredIntegrationEvent>>();

        accountRegisteredOptions.BootstrapServers.Should().Be("broker:9092");
        accountRegisteredOptions.Topic.Should().Be("user-created-topic");
        typedAccountRegisteredOptions.Topic.Should().Be("user-created-topic");
    }

    [Theory]
    [InlineData("AuthService/src/FlowChat.AuthService.API/appsettings.json")]
    [InlineData("AuthService/src/FlowChat.AuthService.API/appsettings.Development.json")]
    [InlineData("AuthService/src/Workers/FlowChat.AuthService.OutboxPublisher/appsettings.json")]
    [InlineData("AuthService/src/Workers/FlowChat.AuthService.OutboxPublisher/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaProducerSections(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var accountRegisteredOptions = configuration
            .GetSection(AccountRegisteredProducerSettingsSection.SectionName)
            .Get<AccountRegisteredProducerSettingsSection>();

        accountRegisteredOptions.Should().NotBeNull();
        accountRegisteredOptions!.BootstrapServers.Should().Be("localhost:9092");
        accountRegisteredOptions.Topic.Should().Be("dev.flowchat.identity.user.v1");
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
