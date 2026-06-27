using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FluentAssertions;
using MediatR;
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
                ITestProjectionBulkRepository,
                ITestProjectionOffsetStore,
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
                ITestProjectionBulkRepository,
                ITestProjectionOffsetStore,
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
    public void AddProjectionBulkConsumer_RegistersProjectionBulkCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork, TestUnitOfWork>();
        services.AddScoped<ITestProjectionBulkRepository, TestProjectionBulkRepository>();
        services.AddScoped<ITestProjectionOffsetStore, TestProjectionOffsetStore>();

        services
            .AddSilverback()
            .AddProjectionBulkConsumer<
                TestDbContext,
                TestReadModel,
                TestProjectionItem,
                TestProjectionCommandItemFactory,
                ITestProjectionBulkRepository,
                ITestProjectionOffsetStore,
                TestBatchSubscriber,
                TestRetrySubscriber>(
                    new TestProjectionBulkConsumerSettingsSection(),
                    "projection-main",
                    "projection-retry");

        using var provider = services.BuildServiceProvider();

        provider
            .GetRequiredService<IRequestHandler<ProjectionBulkCommand<TestProjectionItem>, FlowChatResult<Unit>>>()
            .Should()
            .BeOfType<ProjectionBulkCommandHandlerBaseV2<
                ProjectionBulkCommand<TestProjectionItem>,
                TestProjectionItem,
                ITestProjectionBulkRepository,
                ITestProjectionOffsetStore>>();
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

    private interface ITestProjectionBulkRepository : IProjectionBulkRepository<TestProjectionItem>;

    private sealed class TestProjectionBulkRepository : ITestProjectionBulkRepository
    {
        public Task BulkUpsertOrSoftDeleteAsync(
            IReadOnlyCollection<TestProjectionItem> items,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private interface ITestProjectionOffsetStore : IProjectionOffsetStore;

    private sealed class TestProjectionOffsetStore : ITestProjectionOffsetStore
    {
        public Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class TestUnitOfWork : IUnitOfWork
    {
        public void Dispose()
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken) =>
            operation(cancellationToken);

        public Task<FlowChatResult<T>> ExecuteCommandInTransactionAsync<T>(
            Func<CancellationToken, Task<FlowChatResult<T>>> operation,
            CancellationToken cancellationToken)
            where T : notnull =>
            operation(cancellationToken);
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
