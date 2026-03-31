using System.Collections;
using FlowChat.NotificationService.Consumers;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.NotificationService.UnitTests;

public sealed class NotificationConsumerConfigurationTests
{
    [Fact]
    public async Task AddConsumers_RegistersConsumerInfrastructure()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();
        await using var scope = serviceProvider.CreateAsyncScope();

        var consumerCollection = serviceProvider.GetRequiredService<IConsumerCollection>();
        var subscriber = scope.ServiceProvider.GetRequiredService<UserEmailVerificationRequestedSubscriber>();
        var internalApiClient = scope.ServiceProvider.GetRequiredService<INotificationInternalApiClient>();

        consumerCollection.Should().NotBeNull();
        subscriber.Should().NotBeNull();
        internalApiClient.Should().NotBeNull();
    }

    [Theory]
    [InlineData("NotificationService/src/Workers/FlowChat.NotificationService.Consumers/appsettings.json")]
    [InlineData("NotificationService/src/Workers/FlowChat.NotificationService.Consumers/appsettings.Development.json")]
    public void AppSettingsFiles_ExposeRequiredKafkaConsumerSection(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var consumerOptions = configuration
            .GetSection(UserEmailVerificationRequestedConsumerOptions.SectionName)
            .Get<UserEmailVerificationRequestedConsumerOptions>();

        consumerOptions.Should().NotBeNull();
        consumerOptions!.GroupId.Should().Be("notification-service");
        consumerOptions.RetryGroupId.Should().Be("notification-service-retry");
        consumerOptions.Topic.Should().Be("dev.flowchat.notification.email.v1");
        consumerOptions.RetryTopic.Should().Be("dev.flowchat.notification.email.v1.retry");
        consumerOptions.DeadLetterTopic.Should().Be("dev.flowchat.notification.email.v1.dlq");
    }

    [Fact]
    public async Task AddConsumers_RegistersNotificationInternalApiNamedClient()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var internalApiClient = serviceProvider.GetRequiredService<INotificationInternalApiClient>();
        var httpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(NotificationInternalApiClient.HttpClientName);

        internalApiClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().Be(new Uri("https://localhost:7206"));
        httpClient.DefaultRequestHeaders.GetValues(NotificationInternalApiClient.ApiKeyHeaderName).Single()
            .Should().Be("worker-key");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NotificationApi:BaseUrl"] = "https://localhost:7206",
                ["NotificationApi:ApiKey"] = "worker-key",
                ["Kafka:UserEmailVerificationRequestedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:UserEmailVerificationRequestedConsumer:GroupId"] = "notification-service",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryGroupId"] = "notification-service-retry",
                ["Kafka:UserEmailVerificationRequestedConsumer:Topic"] = "dev.flowchat.notification.email.v1",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryTopic"] = "dev.flowchat.notification.email.v1.retry",
                ["Kafka:UserEmailVerificationRequestedConsumer:DeadLetterTopic"] = "dev.flowchat.notification.email.v1.dlq",
                ["Kafka:UserEmailVerificationRequestedConsumer:MaxRetryCount"] = "5",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:UserEmailVerificationRequestedConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:UserEmailVerificationRequestedConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();
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
