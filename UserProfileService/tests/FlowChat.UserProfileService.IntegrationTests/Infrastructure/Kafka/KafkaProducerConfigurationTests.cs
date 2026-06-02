using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.UserProfileService.Infrastructure;
using FlowChat.UserProfileService.Infrastructure.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
                ["Kafka:UserEmailConfirmedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserEmailConfirmedProducer:Topic"] = "user-email-confirmed-topic",
                ["Kafka:UserEmailVerificationRequestedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserEmailVerificationRequestedProducer:Topic"] = "user-email-verification-topic",
                ["Kafka:UserProfileProjectionProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserProfileProjectionProducer:Topic"] = "user-profile-projection-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var emailConfirmedProducerOptions = serviceProvider.GetRequiredService<IOptions<UserEmailConfirmedProducerSettingsSection>>().Value;
        var emailVerificationRequestedProducerOptions = serviceProvider.GetRequiredService<IOptions<UserEmailVerificationRequestedProducerSettingsSection>>().Value;
        var projectionProducerOptions = serviceProvider.GetRequiredService<IOptions<UserProfileProjectionProducerSettingsSection>>().Value;
        var registry = serviceProvider.GetRequiredService<KafkaProducerSettingsRegistry>();
        var typedEmailConfirmedProducerOptions = registry.Get<UserEmailConfirmedIntegrationEvent>();
        var typedEmailVerificationRequestedProducerOptions = registry.Get<EmailVerificationRequestIntegrationEvent>();
        var typedProjectionProducerOptions = registry.Get<ProjectionIntegrationEvent<UserProfileReadModel>>();

        emailConfirmedProducerOptions.BootstrapServers.Should().Be("broker:9092");
        emailConfirmedProducerOptions.Topic.Should().Be("user-email-confirmed-topic");
        emailVerificationRequestedProducerOptions.BootstrapServers.Should().Be("broker:9092");
        emailVerificationRequestedProducerOptions.Topic.Should().Be("user-email-verification-topic");
        projectionProducerOptions.BootstrapServers.Should().Be("broker:9092");
        projectionProducerOptions.Topic.Should().Be("user-profile-projection-topic");
        typedEmailConfirmedProducerOptions!.Topic.Should().Be("user-email-confirmed-topic");
        typedEmailVerificationRequestedProducerOptions!.Topic.Should().Be("user-email-verification-topic");
        typedProjectionProducerOptions!.Topic.Should().Be("user-profile-projection-topic");
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

        var emailConfirmedProducerOptions = configuration
            .GetSection(new UserEmailConfirmedProducerSettingsSection().SectionName)
            .Get<UserEmailConfirmedProducerSettingsSection>();
        var emailVerificationRequestedProducerOptions = configuration
            .GetSection(new UserEmailVerificationRequestedProducerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedProducerSettingsSection>();
        var projectionProducerOptions = configuration
            .GetSection(new UserProfileProjectionProducerSettingsSection().SectionName)
            .Get<UserProfileProjectionProducerSettingsSection>();

        emailConfirmedProducerOptions.Should().NotBeNull();
        emailVerificationRequestedProducerOptions.Should().NotBeNull();
        projectionProducerOptions.Should().NotBeNull();
        emailConfirmedProducerOptions!.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
        emailVerificationRequestedProducerOptions!.Topic.Should().Be("dev.flowchat.notification.email.v1");
        projectionProducerOptions!.Topic.Should().Be("dev.flowchat.user-profile.user-profile.v1");
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
