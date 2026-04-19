using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.AuthService.Events;
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
                ["Kafka:AccountRegisteredProducer:Topic"] = "user-created-topic",
                ["Kafka:AccountConfirmedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:AccountConfirmedProducer:Topic"] = "account-confirmed-topic",
                ["Kafka:PhoneNumberConfirmedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:PhoneNumberConfirmedProducer:Topic"] = "phone-number-confirmed-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var settingsProvider = serviceProvider.GetRequiredService<ISettingsProvider>();
        var accountRegisteredOptions = settingsProvider.GetSection<AccountRegisteredProducerSettingsSection>();
        var accountConfirmedOptions = settingsProvider.GetSection<AccountConfirmedProducerSettingsSection>();
        var phoneNumberConfirmedOptions = settingsProvider.GetSection<PhoneNumberConfirmedProducerSettingsSection>();
        var typedAccountRegisteredOptions = serviceProvider
            .GetRequiredService<IKafkaProducerSettingsSection<AccountRegisteredIntegrationEvent>>();
        var typedAccountConfirmedOptions = serviceProvider
            .GetRequiredService<IKafkaProducerSettingsSection<AccountConfirmedIntegrationEvent>>();
        var typedPhoneNumberConfirmedOptions = serviceProvider
            .GetRequiredService<IKafkaProducerSettingsSection<PhoneNumberConfirmedIntegrationEvent>>();

        accountRegisteredOptions.BootstrapServers.Should().Be("broker:9092");
        accountRegisteredOptions.Topic.Should().Be("user-created-topic");
        accountConfirmedOptions.BootstrapServers.Should().Be("broker:9092");
        accountConfirmedOptions.Topic.Should().Be("account-confirmed-topic");
        phoneNumberConfirmedOptions.BootstrapServers.Should().Be("broker:9092");
        phoneNumberConfirmedOptions.Topic.Should().Be("phone-number-confirmed-topic");
        typedAccountRegisteredOptions.Topic.Should().Be("user-created-topic");
        typedAccountConfirmedOptions.Topic.Should().Be("account-confirmed-topic");
        typedPhoneNumberConfirmedOptions.Topic.Should().Be("phone-number-confirmed-topic");
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
            .GetSection(new AccountRegisteredProducerSettingsSection().SectionName)
            .Get<AccountRegisteredProducerSettingsSection>();
        var accountConfirmedOptions = configuration
            .GetSection(new AccountConfirmedProducerSettingsSection().SectionName)
            .Get<AccountConfirmedProducerSettingsSection>();
        var phoneNumberConfirmedOptions = configuration
            .GetSection(new PhoneNumberConfirmedProducerSettingsSection().SectionName)
            .Get<PhoneNumberConfirmedProducerSettingsSection>();

        accountRegisteredOptions.Should().NotBeNull();
        accountConfirmedOptions.Should().NotBeNull();
        phoneNumberConfirmedOptions.Should().NotBeNull();
        accountRegisteredOptions!.BootstrapServers.Should().Be("localhost:9092");
        accountRegisteredOptions.Topic.Should().Be("dev.flowchat.identity.user.v1");
        accountConfirmedOptions!.BootstrapServers.Should().Be("localhost:9092");
        accountConfirmedOptions.Topic.Should().Be("dev.flowchat.identity.user.v1");
        phoneNumberConfirmedOptions!.BootstrapServers.Should().Be("localhost:9092");
        phoneNumberConfirmedOptions.Topic.Should().Be("dev.flowchat.identity.user.v1");
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
