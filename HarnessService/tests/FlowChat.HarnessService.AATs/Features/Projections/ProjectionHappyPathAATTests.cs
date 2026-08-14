using FlowChat.Core.Messaging;
using FlowChat.HarnessService.AATs.Infrastructure;
using FluentAssertions;

namespace FlowChat.HarnessService.AATs.Features.Projections;

/// <summary>
/// Uses Kafka and PostgreSQL Testcontainers.
/// Run with: dotnet test --filter Category=AAT
/// </summary>
[Collection(HarnessAATCollectionFixture.CollectionName)]
[Trait("Category", "AAT")]
public sealed class ProjectionHappyPathAATTests : IAsyncLifetime
{
    private readonly HarnessAATCollectionFixture _fixture;
    private HarnessConsumerHost _consumerHost = null!;
    private KafkaTestPublisher _publisher = null!;

    public ProjectionHappyPathAATTests(HarnessAATCollectionFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _consumerHost = new HarnessConsumerHost(
            _fixture.ConnectionString,
            _fixture.BootstrapServers,
            _fixture.Projection);
        await _consumerHost.InitializeAsync();

        _publisher = new KafkaTestPublisher(
            _fixture.BootstrapServers,
            _fixture.Projection.Topic);
        await _publisher.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _publisher.DisposeAsync();
        await _consumerHost.DisposeAsync();
    }

    [Fact]
    public async Task PublishSingleEvent_ProjectsRowToDatabase()
    {
        var id = Guid.NewGuid();
        var payload = "hello-world";

        await _publisher.PublishAsync(id, payload, version: 1);

        var rows = await DbPoller.WaitForRowsAsync(
            _fixture.ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().ContainSingle();
        var row = rows[0];
        row.Id.Should().Be(id);
        row.Payload.Should().Be(payload);
        row.SourceVersion.Should().Be(1);
    }

    [Fact]
    public async Task PublishBatch_AllValidItems_AllProjectedToDatabase()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();

        foreach (var id in ids)
        {
            await _publisher.PublishAsync(id, $"payload-{id:N}", version: 1);
        }

        var rows = await DbPoller.WaitForRowsAsync(
            _fixture.ConnectionString,
            ids,
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().HaveCount(5);
        rows.Select(r => r.Id).Should().BeEquivalentTo(ids);
    }

    [Fact]
    public async Task PublishDuplicateVersions_KeepsOnlyLatestVersion()
    {
        var id = Guid.NewGuid();
        var initialOffset = await DbPoller.GetStoredOffsetAsync(
            _fixture.ConnectionString,
            _consumerHost.ProjectionMainGroupId,
            _fixture.Projection.Topic);

        await _publisher.PublishAsync(id, "v1-payload", version: 1);
        await _publisher.PublishAsync(id, "v3-payload", version: 3);
        await _publisher.PublishAsync(id, "v2-payload", version: 2);

        await DbPoller.WaitForStoredOffsetAsync(
            _fixture.ConnectionString,
            _consumerHost.ProjectionMainGroupId,
            _fixture.Projection.Topic,
            minimumOffset: initialOffset + 3,
            timeout: TimeSpan.FromSeconds(30));
        var row = await DbPoller.WaitForRowAsync(
            _fixture.ConnectionString,
            id,
            candidate => candidate.SourceVersion == 3,
            TimeSpan.FromSeconds(30));

        row.SourceVersion.Should().Be(3);
        row.Payload.Should().Be("v3-payload");
    }

    [Fact]
    public async Task PublishDeletedOperation_CreatesTombstoneInDatabase()
    {
        var id = Guid.NewGuid();

        await _publisher.PublishAsync(id, "some-payload", version: 1, operation: OperationType.Created);
        await _publisher.PublishAsync(id, string.Empty, version: 2, operation: OperationType.Deleted);

        var row = await DbPoller.WaitForRowAsync(
            _fixture.ConnectionString,
            id,
            candidate => candidate.SourceDeletedAtUtc is not null,
            TimeSpan.FromSeconds(30));

        row.SourceDeletedAtUtc.Should().NotBeNull();
    }
}
