using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.Core.Messaging.AuthService.Events;
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
                ["Kafka:UserCreatedConsumer:BootstrapServers"] = "legacy-broker:9092",
                ["Kafka:UserCreatedConsumer:Topic"] = "legacy-user-created-topic",
                ["Kafka:UserCreatedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserCreatedProducer:Topic"] = "user-created-topic",
                ["Kafka:UserEmailVerificationRequestedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserEmailVerificationRequestedProducer:Topic"] = "email-verification-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var settingsManager = serviceProvider.GetRequiredService<IWorkerSettingsManager>();
        var userCreatedOptions = settingsManager.GetUserCreatedProducerOptions();
        var emailVerificationOptions = settingsManager.GetUserEmailVerificationRequestedProducerOptions();
        var typedUserCreatedOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<UserCreatedIntegrationEvent>>();
        var typedEmailVerificationOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<EmailVerificationRequestIntegrationEvent>>();

        Assert.Equal("broker:9092", userCreatedOptions.BootstrapServers);
        Assert.Equal("user-created-topic", userCreatedOptions.Topic);
        Assert.Equal("broker:9092", emailVerificationOptions.BootstrapServers);
        Assert.Equal("email-verification-topic", emailVerificationOptions.Topic);
        Assert.Equal("user-created-topic", typedUserCreatedOptions.Topic);
        Assert.Equal("email-verification-topic", typedEmailVerificationOptions.Topic);
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

        var userCreatedOptions = configuration
            .GetSection(UserCreatedProducerOptions.SectionName)
            .Get<UserCreatedProducerOptions>();
        var emailVerificationOptions = configuration
            .GetSection(UserEmailVerificationRequestedProducerOptions.SectionName)
            .Get<UserEmailVerificationRequestedProducerOptions>();

        Assert.NotNull(userCreatedOptions);
        Assert.NotNull(emailVerificationOptions);
        Assert.Equal("localhost:9092", userCreatedOptions!.BootstrapServers);
        Assert.Equal("dev.flowchat.identity.user.v1", userCreatedOptions.Topic);
        Assert.Equal("localhost:9092", emailVerificationOptions!.BootstrapServers);
        Assert.Equal("dev.flowchat.notification.email.v1", emailVerificationOptions.Topic);
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
