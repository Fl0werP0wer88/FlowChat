using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.Projections.Bulk;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.ProjectionBulk;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace FlowChat.Shared.Consumers.UnitTests.Projections.Bulk;

public sealed class ProjectionBulkServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddProjectionBulk_RegistersOnlyMainConsumerAndNoProducers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.AddSingleton(Mock.Of<IHostApplicationLifetime>());

        services
            .AddSilverback()
            .AddProjectionBulk(
                new TestProjectionBulkConsumerSettingsSection(),
                "projection-main",
                bulkBuilder => bulkBuilder.AddConsumer<
                    TestDbContext,
                    TestReadModel,
                    TestProjectionValue,
                    Guid,
                    TestProjectionValueFactory>());

        await using var provider = services.BuildServiceProvider();
        await provider.GetRequiredService<IBrokerClientsConnector>().InitializeAsync();

        var consumers = provider.GetRequiredService<IConsumerCollection>()
            .Cast<KafkaConsumer>()
            .ToArray();
        var producers = provider.GetRequiredService<IProducerCollection>();

        consumers.Should().ContainSingle();
        consumers[0].Configuration.GroupId.Should().Be("test-main-group");
        consumers[0].Configuration.Endpoints
            .SelectMany(endpoint => endpoint.TopicPartitions)
            .Should()
            .ContainSingle(topicPartition => topicPartition.Topic == "test-topic");
        producers.Should().BeEmpty();
    }

    [Fact]
    public void AddProjectionBulk_RegistersProjectionValueFactory()
    {
        var services = new ServiceCollection();

        services
            .AddSilverback()
            .AddProjectionBulk(
                new TestProjectionBulkConsumerSettingsSection(),
                "projection-main",
                bulkBuilder => bulkBuilder.AddConsumer<
                    TestDbContext,
                    TestReadModel,
                    TestProjectionValue,
                    Guid,
                    TestProjectionValueFactory>());

        using var provider = services.BuildServiceProvider();

        provider
            .GetRequiredService<IProjectionValueFactory<TestReadModel, TestProjectionValue, Guid>>()
            .Should()
            .BeOfType<TestProjectionValueFactory>();
    }

    [Fact]
    public void AddProjectionBulk_RegistersSubscriberWrappers()
    {
        var services = new ServiceCollection();

        services
            .AddSilverback()
            .AddProjectionBulk(
                new TestProjectionBulkConsumerSettingsSection(),
                "projection-main",
                bulkBuilder => bulkBuilder.AddConsumer<
                    TestDbContext,
                    TestReadModel,
                    TestProjectionValue,
                    Guid,
                    TestProjectionValueFactory>());

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(ProjectionBatchSubscriber<TestReadModel, TestProjectionValue, Guid>));
    }

    [Fact]
    public void AddProjectionBulk_RegistersConfiguredPipeline()
    {
        var services = new ServiceCollection();

        services
            .AddSilverback()
            .AddProjectionBulk(
                new TestProjectionBulkConsumerSettingsSection(),
                "projection-main",
                bulkBuilder => bulkBuilder
                    .AddRepository<
                        TestDbContext,
                        TestProjectionValue,
                        TestProjectionEntity,
                        TestProjectionBulkEntityFactory>()
                    .AddCommandHandler<TestProjectionValue>()
                    .AddConsumer<
                        TestDbContext,
                        TestReadModel,
                        TestProjectionValue,
                        Guid,
                        TestProjectionValueFactory>());

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IProjectionBulkRepository<ProjectionCommandItem<TestProjectionValue>>)
            && descriptor.ImplementationType == typeof(ProjectionBulkRepository<
                TestDbContext,
                TestProjectionValue,
                TestProjectionEntity,
                TestProjectionBulkEntityFactory>));
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IRequestHandler<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>, FlowChatResult<Unit>>));
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(ProjectionBatchSubscriber<TestReadModel, TestProjectionValue, Guid>));
    }

    [Fact]
    public void AddProjectionBulk_RegistersProjectionBulkCommandHandler()
    {
        var services = new ServiceCollection();
        services.AddScoped<IUnitOfWork, TestUnitOfWork>();
        services.AddScoped<IProjectionBulkRepository<ProjectionCommandItem<TestProjectionValue>>, TestProjectionBulkRepository>();

        services
            .AddSilverback()
            .AddProjectionBulk(
                new TestProjectionBulkConsumerSettingsSection(),
                "projection-main",
                bulkBuilder => bulkBuilder.AddCommandHandler<TestProjectionValue>());

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IRequestHandler<ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>, FlowChatResult<Unit>>)
            && descriptor.ImplementationType == typeof(ProjectionBulkCommandHandlerBaseV2<
                ProjectionBulkCommand<ProjectionCommandItem<TestProjectionValue>>,
                ProjectionCommandItem<TestProjectionValue>,
                IProjectionBulkRepository<ProjectionCommandItem<TestProjectionValue>>>));
    }

    [Fact]
    public void AddProjectionBulk_RegistersProjectionBulkRepository()
    {
        var services = new ServiceCollection();

        services
            .AddSilverback()
            .AddProjectionBulk(
                new TestProjectionBulkConsumerSettingsSection(),
                "projection-main",
                bulkBuilder => bulkBuilder.AddRepository<
                    TestDbContext,
                    TestProjectionValue,
                    TestProjectionEntity,
                    TestProjectionBulkEntityFactory>());

        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IProjectionBulkEntityFactory<TestProjectionValue, TestProjectionEntity>)
            && descriptor.ImplementationType == typeof(TestProjectionBulkEntityFactory));
        services.Should().ContainSingle(descriptor =>
            descriptor.ServiceType == typeof(IProjectionBulkRepository<ProjectionCommandItem<TestProjectionValue>>)
            && descriptor.ImplementationType == typeof(ProjectionBulkRepository<
                TestDbContext,
                TestProjectionValue,
                TestProjectionEntity,
                TestProjectionBulkEntityFactory>));
    }

    private sealed class TestDbContext : DbContext;

    private sealed class TestProjectionValueFactory
        : IProjectionValueFactory<TestReadModel, TestProjectionValue, Guid>
    {
        public TestProjectionValue MapValue(ProjectionIntegrationEvent<TestReadModel> message) =>
            new(message.SourceAggregateId);

        public Guid GetDeduplicationKey(TestProjectionValue value) =>
            value.Id;
    }

    private sealed class TestProjectionBulkRepository : IProjectionBulkRepository<ProjectionCommandItem<TestProjectionValue>>
    {
        public Task BulkUpsertOrSoftDeleteAsync(
            IReadOnlyCollection<ProjectionCommandItem<TestProjectionValue>> items,
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

    private sealed record TestProjectionValue(Guid Id);

    private sealed class TestProjectionEntity : ReadModelEntityBase
    {
        public Guid Id { get; set; }
    }

    private sealed class TestProjectionBulkEntityFactory
        : IProjectionBulkEntityFactory<TestProjectionValue, TestProjectionEntity>
    {
        public IReadOnlyList<string> UpdateByProperties { get; } = [nameof(TestProjectionEntity.Id)];

        public TestProjectionEntity CreateUpsertEntity(
            TestProjectionValue value,
            int sourceVersion,
            DateTimeOffset sourceCreatedAtUtc,
            DateTimeOffset sourceLastModifiedAtUtc,
            DateTimeOffset? sourceDeletedAtUtc) =>
            new()
            {
                Id = value.Id,
                SourceVersion = sourceVersion,
                SourceCreatedAtUtc = sourceCreatedAtUtc,
                SourceLastModifiedAtUtc = sourceLastModifiedAtUtc,
                SourceDeletedAtUtc = sourceDeletedAtUtc
            };

        public TestProjectionEntity CreateTombstoneEntity(
            ProjectionCommandItem<TestProjectionValue> item,
            DateTimeOffset now) =>
            new()
            {
                Id = item.Value.Id,
                SourceVersion = item.SourceVersion,
                SourceCreatedAtUtc = item.SourceCreatedAtUtc,
                SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
                SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now
            };
    }

    private sealed record TestReadModel(string Payload);

    private sealed class TestProjectionBulkConsumerSettingsSection : IProjectionBulkConsumerSettingsSection
    {
        public string BootstrapServers => "localhost:9092";

        public string GroupId => "test-main-group";

        public string AutoOffsetReset => "Earliest";

        public int BatchSize => 10;

        public int BatchMaxWaitTimeMilliseconds => 100;

        public string Topic => "test-topic";
    }
}
