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

namespace FlowChat.Shared.Infrastructure.IntegrationTests.Silverback.Kafka.Retry.Extensions;

public sealed class SilverbackBuilderTieredRetryExtensionsTests
{
    [Fact]
    public async Task AddFlowChatTieredRetry_ValidStreams_RegistersRuntimeAndKafkaClients()
    {
        var streams = new[] { CreateSettings() };
        var services = CreateServices();
        var builder = services.AddSilverback();

        var result = builder.AddFlowChatTieredRetry<TestDbContext>("localhost:9092", streams);
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
    public void AddFlowChatTieredRetry_CustomRuntimeDependencies_DoesNotReplaceThem()
    {
        var services = CreateServices();
        var timeProvider = new Mock<TimeProvider>().Object;
        var partitionController = Mock.Of<IKafkaRetryPartitionController>();
        services.AddSingleton(timeProvider);
        services.AddSingleton(partitionController);

        services.AddSilverback()
            .AddFlowChatTieredRetry<TestDbContext>("localhost:9092", [CreateSettings()]);

        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<TimeProvider>().Should().BeSameAs(timeProvider);
        serviceProvider.GetRequiredService<IKafkaRetryPartitionController>().Should().BeSameAs(partitionController);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddFlowChatTieredRetry_MissingBootstrapServers_ThrowsArgumentException(string bootstrapServers)
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetry<TestDbContext>(bootstrapServers, [CreateSettings()]);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddFlowChatTieredRetry_EmptyStreams_ThrowsArgumentException()
    {
        var builder = CreateServices().AddSilverback();

        var action = () => builder.AddFlowChatTieredRetry<TestDbContext>(
            "localhost:9092",
            Array.Empty<ITieredRetryKafkaConsumerSettingsSection>());

        action.Should().Throw<ArgumentException>().WithMessage("*At least one tiered Kafka retry stream*");
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
