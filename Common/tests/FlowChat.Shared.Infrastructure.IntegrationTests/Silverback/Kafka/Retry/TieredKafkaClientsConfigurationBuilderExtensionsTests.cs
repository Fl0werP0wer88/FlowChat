using Confluent.Kafka;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.Shared.Infrastructure.IntegrationTests.Silverback.Kafka.Retry;

public sealed class TieredKafkaClientsConfigurationBuilderExtensionsTests
{
    [Fact]
    public async Task AddFlowChatTieredRetryStream_FourTiers_RegistersCompleteEfBackedTopology()
    {
        var settings = CreateSettings();
        await using var serviceProvider = CreateServiceProvider(settings);

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        var consumers = serviceProvider.GetRequiredService<IConsumerCollection>()
            .Cast<KafkaConsumer>()
            .ToArray();
        var producers = serviceProvider.GetRequiredService<IProducerCollection>();

        consumers.Should().HaveCount(5);
        consumers.Should().ContainSingle(consumer => consumer.Configuration.GroupId == settings.GroupId);
        consumers.Count(consumer => consumer.Configuration.GroupId == settings.RetryGroupId).Should().Be(4);
        consumers.Should().AllSatisfy(consumer =>
        {
            consumer.Configuration.CommitOffsets.Should().BeFalse();
            consumer.Configuration.ClientSideOffsetStore.Should()
                .BeOfType<EntityFrameworkKafkaOffsetStoreSettings>()
                .Which.DbContextType.Should().Be<TestDbContext>();
        });

        var consumerTopics = consumers
            .SelectMany(consumer => consumer.Configuration.Endpoints)
            .SelectMany(endpoint => endpoint.TopicPartitions)
            .Select(topicPartition => topicPartition.Topic);
        consumerTopics.Should().BeEquivalentTo(
            settings.RetryTiers.Select(tier => tier.Topic).Prepend(settings.Topic));

        var destinationTopics = settings.RetryTiers
            .Select(tier => tier.Topic)
            .Append(settings.DeadLetterTopic)
            .ToArray();
        producers.Should().HaveCount(5);
        destinationTopics.Should().AllSatisfy(topic =>
        {
            var producer = producers.GetProducerForEndpoint(topic);
            producer.EndpointConfiguration.Strategy.Should()
                .BeOfType<OutboxProduceStrategy>()
                .Which.Settings.Should()
                .BeOfType<EntityFrameworkOutboxSettings>()
                .Which.DbContextType.Should().Be<TestDbContext>();
        });
    }

    [Fact]
    public async Task AddFlowChatTieredRetryStream_InvalidAutoOffsetReset_FallsBackToEarliest()
    {
        var settings = CreateSettings() with { AutoOffsetReset = "invalid" };
        await using var serviceProvider = CreateServiceProvider(settings);

        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        var consumers = serviceProvider.GetRequiredService<IConsumerCollection>()
            .Cast<KafkaConsumer>();

        consumers.Should().AllSatisfy(consumer =>
            consumer.Configuration.AutoOffsetReset.Should().Be(AutoOffsetReset.Earliest));
    }

    private static ServiceProvider CreateServiceProvider(TestTieredRetrySettings settings)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        services.AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkOutbox())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers(settings.BootstrapServers)
                .AddFlowChatTieredRetryStream<TestDbContext>(settings));

        return services.BuildServiceProvider();
    }

    private static TestTieredRetrySettings CreateSettings() => new()
    {
        RetryTiers =
        [
            new RetryTierSettings { Topic = "retry-5s", Delay = TimeSpan.FromSeconds(5) },
            new RetryTierSettings { Topic = "retry-20s", Delay = TimeSpan.FromSeconds(20) },
            new RetryTierSettings { Topic = "retry-60s", Delay = TimeSpan.FromSeconds(60) },
            new RetryTierSettings { Topic = "retry-300s", Delay = TimeSpan.FromSeconds(300) }
        ]
    };

    private sealed record TestTieredRetrySettings : ITieredRetryKafkaConsumerSettingsSection
    {
        public string BootstrapServers { get; init; } = "localhost:9092";
        public string GroupId { get; init; } = "main-group";
        public string RetryGroupId { get; init; } = "retry-group";
        public string Topic { get; init; } = "main-topic";
        public string DeadLetterTopic { get; init; } = "dlq-topic";
        public IReadOnlyList<RetryTierSettings> RetryTiers { get; init; } = [];
        public string AutoOffsetReset { get; init; } = "Latest";
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
