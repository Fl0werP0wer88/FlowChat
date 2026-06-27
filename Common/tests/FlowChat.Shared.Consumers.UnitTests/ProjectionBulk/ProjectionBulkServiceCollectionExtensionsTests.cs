using FlowChat.Core.Messaging;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

namespace FlowChat.Shared.Consumers.UnitTests.ProjectionBulk;

public sealed class ProjectionBulkServiceCollectionExtensionsTests
{
    [Fact]
    public void AddProjectionBulkConsumer_RegistersProjectionCommandItemFactory()
    {
        var services = new ServiceCollection();

        services
            .AddSilverback()
            .AddProjectionBulkConsumer<
                TestDbContext,
                TestReadModel,
                TestProjectionItem,
                TestProjectionCommandItemFactory,
                TestBatchSubscriber,
                TestRetrySubscriber>(
                    new TestProjectionBulkConsumerSettingsSection(),
                    "projection-main",
                    "projection-retry");

        using var provider = services.BuildServiceProvider();

        provider
            .GetRequiredService<IProjectionCommandItemFactory<TestReadModel, TestProjectionItem>>()
            .Should()
            .BeOfType<TestProjectionCommandItemFactory>();
    }

    [Fact]
    public void AddProjectionBulkConsumer_RegistersSubscriberWrappers()
    {
        var services = new ServiceCollection();

        services
            .AddSilverback()
            .AddProjectionBulkConsumer<
                TestDbContext,
                TestReadModel,
                TestProjectionItem,
                TestProjectionCommandItemFactory,
                TestBatchSubscriber,
                TestRetrySubscriber>(
                    new TestProjectionBulkConsumerSettingsSection(),
                    "projection-main",
                    "projection-retry");

        using var provider = services.BuildServiceProvider();

        provider.GetServices<TestBatchSubscriber>().Should().ContainSingle();
        provider.GetServices<TestRetrySubscriber>().Should().ContainSingle();
    }

    [Fact]
    public void ProjectionBulkDeadLetterSentinel_IsAvailableFromCommon()
    {
        var sentinel = new ProjectionBulkDeadLetterSentinel();

        sentinel.Should().NotBeNull();
    }

    private sealed class TestDbContext : DbContext;

    private sealed class TestProjectionCommandItemFactory
        : IProjectionCommandItemFactory<TestReadModel, TestProjectionItem>
    {
        public TestProjectionItem MapItem(ProjectionIntegrationEvent<TestReadModel> message) =>
            new(message.SourceAggregateId);
    }

    private sealed class TestBatchSubscriber;

    private sealed class TestRetrySubscriber;

    private sealed record TestProjectionItem(Guid Id);

    private sealed record TestReadModel(string Payload);

    private sealed class TestProjectionBulkConsumerSettingsSection : IProjectionBulkConsumerSettingsSection
    {
        public string BootstrapServers => "localhost:9092";

        public string GroupId => "test-main-group";

        public string RetryGroupId => "test-retry-group";

        public string AutoOffsetReset => "Earliest";

        public int BatchSize => 10;

        public int BatchMaxWaitTimeMilliseconds => 100;

        public string Topic => "test-topic";

        public string RetryTopic => "test-retry-topic";

        public string DeadLetterTopic => "test-dlq-topic";

        public int MaxRetryCount => 3;

        public int RetryBaseDelaySeconds => 1;

        public int RetryMaxDelaySeconds => 10;
    }
}
