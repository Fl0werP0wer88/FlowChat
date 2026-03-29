using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.UnitTests;

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

        Assert.Equal("broker:9092", createdProducerOptions.BootstrapServers);
        Assert.Equal("user-profile-created-topic", createdProducerOptions.Topic);
        Assert.Equal("broker:9092", emailConfirmedProducerOptions.BootstrapServers);
        Assert.Equal("user-email-confirmed-topic", emailConfirmedProducerOptions.Topic);
        Assert.Equal("broker:9092", emailVerificationRequestedProducerOptions.BootstrapServers);
        Assert.Equal("user-email-verification-topic", emailVerificationRequestedProducerOptions.Topic);
        Assert.Equal("broker:9092", stateChangedProducerOptions.BootstrapServers);
        Assert.Equal("user-profile-state-topic", stateChangedProducerOptions.Topic);
        Assert.Equal("user-profile-created-topic", typedCreatedProducerOptions.Topic);
        Assert.Equal("user-email-confirmed-topic", typedEmailConfirmedProducerOptions.Topic);
        Assert.Equal("user-email-verification-topic", typedEmailVerificationRequestedProducerOptions.Topic);
        Assert.Equal("user-profile-state-topic", typedStateChangedProducerOptions.Topic);
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

        Assert.NotNull(producerOptions);
        Assert.NotNull(emailConfirmedProducerOptions);
        Assert.NotNull(emailVerificationRequestedProducerOptions);
        Assert.NotNull(stateChangedProducerOptions);
        Assert.Equal("dev.flowchat.user-profile.user-profile.v1", producerOptions!.Topic);
        Assert.Equal("dev.flowchat.user-profile.user-profile.v1", emailConfirmedProducerOptions!.Topic);
        Assert.Equal("dev.flowchat.notification.email.v1", emailVerificationRequestedProducerOptions!.Topic);
        Assert.Equal("dev.flowchat.user-profile.user-profile.v1", stateChangedProducerOptions!.Topic);
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
