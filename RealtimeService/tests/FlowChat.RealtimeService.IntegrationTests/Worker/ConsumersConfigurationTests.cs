using FlowChat.RealtimeService.Consumers;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConsumersConfigurationTests
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
        var chatSubscriber = scope.ServiceProvider.GetRequiredService<ChatMessageSentSubscriber>();
        var presenceSubscriber = scope.ServiceProvider.GetRequiredService<UserPresenceChangedSubscriber>();
        var internalApiClient = scope.ServiceProvider.GetRequiredService<IRealtimeInternalApiClient>();

        consumerCollection.Should().NotBeNull();
        chatSubscriber.Should().NotBeNull();
        presenceSubscriber.Should().NotBeNull();
        internalApiClient.Should().NotBeNull();
    }

    [Fact]
    public async Task AddConsumers_RegistersRealtimeInternalApiNamedClientWithBaseAddressAndApiKeyHeader()
    {
        var configuration = CreateConfiguration();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.AddLogging();
        services.AddConsumers(configuration);

        await using var serviceProvider = services.BuildServiceProvider();

        var internalApiClient = serviceProvider.GetRequiredService<IRealtimeInternalApiClient>();
        var httpClient = serviceProvider
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient(RealtimeInternalApiClient.HttpClientName);

        internalApiClient.Should().NotBeNull();
        httpClient.BaseAddress.Should().Be(new Uri("http://localhost:5215"));
        httpClient.DefaultRequestHeaders.GetValues(RealtimeInternalApiClient.ApiKeyHeaderName).Single()
            .Should().Be("worker-key");
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RealtimeApi:ApiKey"] = "worker-key",
                ["RealtimeApi:BaseUrl"] = "http://localhost:5215",
                ["Kafka:ChatMessageSentConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:ChatMessageSentConsumer:GroupId"] = "realtime-service",
                ["Kafka:ChatMessageSentConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:ChatMessageSentConsumer:Topic"] = "dev.flowchat.chat.message.v1",
                ["Kafka:ChatMessageSentConsumer:RetryTopic"] = "dev.flowchat.chat.message.v1.retry",
                ["Kafka:ChatMessageSentConsumer:DeadLetterTopic"] = "dev.flowchat.chat.message.v1.dlq",
                ["Kafka:ChatMessageSentConsumer:MaxRetryCount"] = "5",
                ["Kafka:ChatMessageSentConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:ChatMessageSentConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:ChatMessageSentConsumer:AutoOffsetReset"] = "Earliest",
                ["Kafka:PresenceStatusChangedConsumer:BootstrapServers"] = "localhost:9092",
                ["Kafka:PresenceStatusChangedConsumer:GroupId"] = "realtime-service",
                ["Kafka:PresenceStatusChangedConsumer:RetryGroupId"] = "realtime-service-retry",
                ["Kafka:PresenceStatusChangedConsumer:Topic"] = "dev.flowchat.presence.presence",
                ["Kafka:PresenceStatusChangedConsumer:RetryTopic"] = "dev.flowchat.presence.presence.retry",
                ["Kafka:PresenceStatusChangedConsumer:DeadLetterTopic"] = "dev.flowchat.presence.presence.dlq",
                ["Kafka:PresenceStatusChangedConsumer:MaxRetryCount"] = "5",
                ["Kafka:PresenceStatusChangedConsumer:RetryBaseDelaySeconds"] = "5",
                ["Kafka:PresenceStatusChangedConsumer:RetryMaxDelaySeconds"] = "300",
                ["Kafka:PresenceStatusChangedConsumer:AutoOffsetReset"] = "Earliest"
            })
            .Build();
    }
}
