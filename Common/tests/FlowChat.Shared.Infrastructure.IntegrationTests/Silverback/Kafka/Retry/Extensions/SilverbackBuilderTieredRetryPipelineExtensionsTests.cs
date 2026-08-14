using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.Shared.Infrastructure.IntegrationTests.Silverback.Kafka.Retry.Extensions;

public sealed class SilverbackBuilderTieredRetryPipelineExtensionsTests
{
    [Fact]
    public async Task AddFlowChatTieredRetryConsumerPipeline_ValidStreams_RegistersRuntimeAndKafkaClients()
    {
        var streams = new[] { CreateSettings() };
        var services = CreateServices();
        var builder = services.AddSilverback();

        var result = builder.AddFlowChatTieredRetryConsumerPipeline<TestDbContext>("localhost:9092", streams);
        result.WithConnectionToMessageBroker(options => options
            .AddKafka()
            .AddEntityFrameworkKafkaOffsetStore()
            .AddEntityFrameworkOutbox());

        result.Should().BeSameAs(builder);
        services.Should().ContainSingle(descriptor =>
            descriptor.ImplementationType == typeof(DelayedRetryConsumerBehavior));
        services.Should().ContainSingle(descriptor =>
            descriptor.ImplementationType == typeof(InvalidRetryMetadataConsumerBehavior));

        await using var serviceProvider = services.BuildServiceProvider();
        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();

        var topology = serviceProvider.GetRequiredService<TieredKafkaRetryTopology>();
        topology.Streams.Should().Equal(streams);
        serviceProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
        serviceProvider.GetRequiredService<IKafkaRetryPartitionController>()
            .Should().BeOfType<KafkaRetryPartitionController>();
        serviceProvider.GetRequiredService<IConsumerCollection>().Should().HaveCount(5);
        serviceProvider.GetRequiredService<IProducerCollection>().Should().HaveCount(5);
    }

    [Fact]
    public void AddFlowChatTieredRetryConsumerPipeline_CustomRuntimeDependencies_DoesNotReplaceThem()
    {
        var services = CreateServices();
        var timeProvider = new Mock<TimeProvider>().Object;
        var partitionController = Mock.Of<IKafkaRetryPartitionController>();
        services.AddSingleton(timeProvider);
        services.AddSingleton(partitionController);

        services.AddSilverback()
            .AddFlowChatTieredRetryConsumerPipeline<TestDbContext>("localhost:9092", [CreateSettings()]);

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(timeProvider);
        serviceProvider.GetRequiredService<IKafkaRetryPartitionController>().Should().BeSameAs(partitionController);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddFlowChatTieredRetryConsumerPipeline_MissingBootstrapServers_ThrowsArgumentException(
        string bootstrapServers)
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetryConsumerPipeline<TestDbContext>(
            bootstrapServers,
            [CreateSettings()]);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddFlowChatTieredRetryConsumerPipeline_EmptyStreams_ThrowsArgumentException()
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetryConsumerPipeline<TestDbContext>(
            "localhost:9092",
            Array.Empty<ITieredRetryKafkaConsumerSettingsSection>());

        action.Should().Throw<ArgumentException>().WithMessage("*At least one tiered Kafka retry stream*");
    }

    [Fact]
    public async Task AddFlowChatTieredRetryProducerPipeline_ValidTopics_RegistersNamedProducers()
    {
        string[] topics = ["retry-5s", "retry-20s", "dlq"];
        var services = CreateServices();
        var builder = services.AddSilverback();

        var result = builder.AddFlowChatTieredRetryProducerPipeline("localhost:9092", topics);
        result.WithConnectionToMessageBroker(options => options.AddKafka());

        result.Should().BeSameAs(builder);

        await using var serviceProvider = services.BuildServiceProvider();
        await serviceProvider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();
        var producers = serviceProvider.GetRequiredService<IProducerCollection>();

        producers.Should().HaveCount(topics.Length);
        topics.Should().AllSatisfy(topic =>
        {
            var producer = producers.GetProducerForEndpoint(topic);
            producer.Should().NotBeNull();
            producer.EndpointConfiguration.Strategy.Should().NotBeOfType<OutboxProduceStrategy>();
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddFlowChatTieredRetryProducerPipeline_MissingBootstrapServers_ThrowsArgumentException(
        string bootstrapServers)
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetryProducerPipeline(bootstrapServers, ["retry"]);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddFlowChatTieredRetryProducerPipeline_EmptyTopics_ThrowsArgumentException()
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetryProducerPipeline("localhost:9092", []);

        action.Should().Throw<ArgumentException>().WithMessage("*At least one tiered Kafka retry destination topic*");
    }

    [Fact]
    public void AddFlowChatTieredRetryProducerPipeline_BlankTopic_ThrowsArgumentException()
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetryProducerPipeline("localhost:9092", ["retry", " "]);

        action.Should().Throw<ArgumentException>().WithMessage("*topics cannot be empty*");
    }

    [Fact]
    public void AddFlowChatTieredRetryProducerPipeline_DuplicateTopic_ThrowsArgumentException()
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetryProducerPipeline(
            "localhost:9092",
            ["retry", "retry"]);

        action.Should().Throw<ArgumentException>().WithMessage("*topics must be unique*");
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());
        services.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=:memory:"));
        return services;
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

    private sealed class TestTieredRetrySettings : ITieredRetryKafkaConsumerSettingsSection
    {
        public string BootstrapServers { get; init; } = "localhost:9092";
        public string GroupId { get; init; } = "main-group";
        public string RetryGroupId { get; init; } = "retry-group";
        public string Topic { get; init; } = "main-topic";
        public string DeadLetterTopic { get; init; } = "dlq-topic";
        public IReadOnlyList<RetryTierSettings> RetryTiers { get; init; } = [];
        public string AutoOffsetReset { get; init; } = "Earliest";
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);
}
