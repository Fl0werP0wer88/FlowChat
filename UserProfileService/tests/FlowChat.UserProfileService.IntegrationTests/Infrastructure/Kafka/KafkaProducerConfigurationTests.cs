using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.IntegrationTests;

public sealed class KafkaProducerConfigurationTests
{
    [Fact]
    public void AddInfrastructureServices_ResolvesKafkaProducerOptions_WithoutFallbackToConsumerSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kafka:AccountRegisteredConsumer:BootstrapServers"] = "legacy-broker:9092",
                ["Kafka:AccountRegisteredConsumer:Topic"] = "legacy-topic",
                ["Kafka:UserProfileCreatedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserProfileCreatedProducer:Topic"] = "user-profile-created-topic",
                ["Kafka:UserEmailConfirmedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserEmailConfirmedProducer:Topic"] = "user-email-confirmed-topic",
                ["Kafka:UserEmailVerificationRequestedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserEmailVerificationRequestedProducer:Topic"] = "user-email-verification-topic",
                ["Kafka:UserProfileStateChangedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserProfileStateChangedProducer:Topic"] = "user-profile-state-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var settingsManager = serviceProvider.GetRequiredService<IKafkaSettingsManager>();
        var createdProducerOptions = settingsManager.GetUserProfileCreatedProducerOptions();
        var emailConfirmedProducerOptions = settingsManager.GetUserEmailConfirmedProducerOptions();
        var emailVerificationRequestedProducerOptions = settingsManager.GetUserEmailVerificationRequestedProducerOptions();
        var stateChangedProducerOptions = settingsManager.GetUserProfileStateChangedProducerOptions();
        var typedCreatedProducerOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<UserProfileCreatedIntegrationEvent>>();
        var typedEmailConfirmedProducerOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<UserEmailConfirmedIntegrationEvent>>();
        var typedEmailVerificationRequestedProducerOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<EmailVerificationRequestIntegrationEvent>>();
        var typedStateChangedProducerOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<UserProfileStateChangedIntegrationEvent>>();

        createdProducerOptions.BootstrapServers.Should().Be("broker:9092");
        createdProducerOptions.Topic.Should().Be("user-profile-created-topic");
        emailConfirmedProducerOptions.BootstrapServers.Should().Be("broker:9092");
        emailConfirmedProducerOptions.Topic.Should().Be("user-email-confirmed-topic");
        emailVerificationRequestedProducerOptions.BootstrapServers.Should().Be("broker:9092");
        emailVerificationRequestedProducerOptions.Topic.Should().Be("user-email-verification-topic");
        stateChangedProducerOptions.BootstrapServers.Should().Be("broker:9092");
        stateChangedProducerOptions.Topic.Should().Be("user-profile-state-topic");
        typedCreatedProducerOptions.Topic.Should().Be("user-profile-created-topic");
        typedEmailConfirmedProducerOptions.Topic.Should().Be("user-email-confirmed-topic");
        typedEmailVerificationRequestedProducerOptions.Topic.Should().Be("user-email-verification-topic");
        typedStateChangedProducerOptions.Topic.Should().Be("user-profile-state-topic");
    }

    [Theory]
    [InlineData("UserProfileService/src/FlowChat.UserProfileService.API/appsettings.json")]
    [InlineData("UserProfileService/src/FlowChat.UserProfileService.API/appsettings.Development.json")]
    [InlineData("UserProfileService/src/Workers/FlowChat.UserProfileService.OutboxPublisher/appsettings.json")]
    [InlineData("UserProfileService/src/Workers/FlowChat.UserProfileService.OutboxPublisher/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaProducerSections(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var producerOptions = configuration
            .GetSection(UserProfileCreatedProducerOptions.SectionName)
            .Get<UserProfileCreatedProducerOptions>();
        var emailConfirmedProducerOptions = configuration
            .GetSection(UserEmailConfirmedProducerOptions.SectionName)
            .Get<UserEmailConfirmedProducerOptions>();
        var emailVerificationRequestedProducerOptions = configuration
            .GetSection(UserEmailVerificationRequestedProducerOptions.SectionName)
            .Get<UserEmailVerificationRequestedProducerOptions>();
        var stateChangedProducerOptions = configuration
            .GetSection(UserProfileStateChangedProducerOptions.SectionName)
            .Get<UserProfileStateChangedProducerOptions>();

        producerOptions.Should().NotBeNull();
        emailConfirmedProducerOptions.Should().NotBeNull();
        emailVerificationRequestedProducerOptions.Should().NotBeNull();
        stateChangedProducerOptions.Should().NotBeNull();
        producerOptions!.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
        emailConfirmedProducerOptions!.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
        emailVerificationRequestedProducerOptions!.Topic.Should().Be("dev.flowchat.notification.email.v1");
        stateChangedProducerOptions!.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
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
