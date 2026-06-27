using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using IPublisher = Silverback.Messaging.Publishing.IPublisher;

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

        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(TestBatchSubscriber));
        services.Should().ContainSingle(descriptor => descriptor.ServiceType == typeof(TestRetrySubscriber));
    }

    [Fact]
    public void AddProjectionBulkCommandHandler_RegistersProjectionBulkCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork, TestUnitOfWork>();
        services.AddScoped<ITestProjectionBulkRepository, TestProjectionBulkRepository>();

        services
            .AddSilverback()
            .AddProjectionBulkCommandHandler<
                TestProjectionItem,
                ITestProjectionBulkRepository>();

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IProjectionOffsetStore)
            && descriptor.ImplementationType == typeof(SilverbackProjectionOffsetStore));

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IRequestHandler<ProjectionBulkCommand<TestProjectionItem>, FlowChatResult<Unit>>)
            && descriptor.ImplementationType == typeof(ProjectionBulkCommandHandlerBaseV2<
                ProjectionBulkCommand<TestProjectionItem>,
                TestProjectionItem,
                ITestProjectionBulkRepository>));
    }

    [Fact]
    public void AddProjectionBulkRepository_RegistersProjectionBulkRepository()
    {
        var services = new ServiceCollection();

        services
            .AddSilverback()
            .AddProjectionBulkRepository<
                TestProjectionItem,
                ITestProjectionBulkRepository,
                TestProjectionBulkRepository>();

        using var provider = services.BuildServiceProvider();

        provider
            .GetRequiredService<ITestProjectionBulkRepository>()
            .Should()
            .BeOfType<TestProjectionBulkRepository>();
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

    private sealed class TestBatchSubscriber(
        IMediator mediator,
        IPublisher publisher,
        IProjectionCommandItemFactory<TestReadModel, TestProjectionItem> itemFactory,
        ILogger<TestBatchSubscriber> logger)
        : ProjectionBatchSubscriberBase<TestReadModel, TestProjectionItem>(
            mediator,
            publisher,
            itemFactory,
            logger);

    private sealed class TestRetrySubscriber(
        IMediator mediator,
        IProjectionCommandItemFactory<TestReadModel, TestProjectionItem> itemFactory,
        ILogger<TestRetrySubscriber> logger)
        : ProjectionRetrySubscriberBase<TestReadModel, TestProjectionItem>(
            mediator,
            itemFactory,
            logger);

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
