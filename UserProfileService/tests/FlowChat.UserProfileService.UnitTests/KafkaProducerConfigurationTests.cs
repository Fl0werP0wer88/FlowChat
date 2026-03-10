using FlowChat.Messaging.Contracts.UserProfileService.Events;
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
                ["Kafka:UserProfileCreatedProducer:Topic"] = "user-profile-created-topic"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddOptions();
        services.AddLogging();
        services.AddInfrastructureServices(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var settingsManager = serviceProvider.GetRequiredService<IKafkaSettingsManager>();
        var producerOptions = settingsManager.GetUserProfileCreatedProducerOptions();
        var typedProducerOptions = serviceProvider
            .GetRequiredService<IKafkaProducerOptions<UserProfileCreatedIntegrationEvent>>();

        Assert.Equal("broker:9092", producerOptions.BootstrapServers);
        Assert.Equal("user-profile-created-topic", producerOptions.Topic);
        Assert.Equal("user-profile-created-topic", typedProducerOptions.Topic);
    }

    [Theory]
    [InlineData("UserProfileService/src/FlowChat.UserProfileService.API/appsettings.json")]
    [InlineData("UserProfileService/src/FlowChat.UserProfileService.API/appsettings.Development.json")]
    [InlineData("UserProfileService/src/FlowChat.UserProfileService.Worker/appsettings.json")]
    [InlineData("UserProfileService/src/FlowChat.UserProfileService.Worker/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaProducerSections(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var producerOptions = configuration
            .GetSection(UserProfileCreatedProducerOptions.SectionName)
            .Get<UserProfileCreatedProducerOptions>();

        Assert.NotNull(producerOptions);
        Assert.Equal("dev.flowchat.user-profile.user.v1", producerOptions!.Topic);
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
