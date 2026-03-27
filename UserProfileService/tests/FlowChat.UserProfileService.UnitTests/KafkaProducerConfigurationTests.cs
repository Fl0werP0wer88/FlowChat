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
                ["Kafka:UserCreatedConsumer:BootstrapServers"] = "legacy-broker:9092",
                ["Kafka:UserCreatedConsumer:Topic"] = "legacy-topic",
                ["Kafka:UserProfileCreatedProducer:BootstrapServers"] = "broker:9092",
                ["Kafka:UserProfileCreatedProducer:Topic"] = "user-profile-created-topic",
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
        var stateChangedProducerOptions = settingsManager.GetUserProfileStateChangedProducerOptions();
        var typedCreatedProducerOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<UserProfileCreatedIntegrationEvent>>();
        var typedStateChangedProducerOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<UserProfileStateChangedIntegrationEvent>>();

        Assert.Equal("broker:9092", createdProducerOptions.BootstrapServers);
        Assert.Equal("user-profile-created-topic", createdProducerOptions.Topic);
        Assert.Equal("broker:9092", stateChangedProducerOptions.BootstrapServers);
        Assert.Equal("user-profile-state-topic", stateChangedProducerOptions.Topic);
        Assert.Equal("user-profile-created-topic", typedCreatedProducerOptions.Topic);
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
        var stateChangedProducerOptions = configuration
            .GetSection(UserProfileStateChangedProducerOptions.SectionName)
            .Get<UserProfileStateChangedProducerOptions>();

        Assert.NotNull(producerOptions);
        Assert.NotNull(stateChangedProducerOptions);
        Assert.Equal("dev.flowchat.user-profile.user-profile.v1", producerOptions!.Topic);
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
