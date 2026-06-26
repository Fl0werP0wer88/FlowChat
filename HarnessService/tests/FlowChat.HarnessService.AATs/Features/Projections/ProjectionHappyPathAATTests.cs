using FlowChat.Core.Messaging;
using FlowChat.HarnessService.AATs.Infrastructure;
using FluentAssertions;

namespace FlowChat.HarnessService.AATs.Features.Projections;

/// <summary>
/// Requires running dev stack (Kafka + PostgreSQL).
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
            _fixture.ApiBaseUrl,
            HarnessAATCollectionFixture.ApiKey,
            HarnessAATCollectionFixture.BootstrapServers,
            HarnessAATCollectionFixture.Topic,
            HarnessAATCollectionFixture.RetryTopic,
            HarnessAATCollectionFixture.DeadLetterTopic);
        await _consumerHost.InitializeAsync();

        _publisher = new KafkaTestPublisher(
            HarnessAATCollectionFixture.BootstrapServers,
            HarnessAATCollectionFixture.Topic);
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
            HarnessAATCollectionFixture.ConnectionString,
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
            HarnessAATCollectionFixture.ConnectionString,
            ids,
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().HaveCount(5);
        rows.Select(r => r.Id).Should().BeEquivalentTo(ids);
    }

    [Fact]
    public async Task PublishDuplicateVersions_KeepsOnlyLatestVersion()
    {
        var id = Guid.NewGuid();

        await _publisher.PublishAsync(id, "v1-payload", version: 1);
        await _publisher.PublishAsync(id, "v3-payload", version: 3);
        await _publisher.PublishAsync(id, "v2-payload", version: 2);

        await Task.Delay(TimeSpan.FromSeconds(5));

        var rows = await DbPoller.WaitForRowsAsync(
            HarnessAATCollectionFixture.ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().ContainSingle();
        rows[0].SourceVersion.Should().Be(3);
        rows[0].Payload.Should().Be("v3-payload");
    }

    [Fact]
    public async Task PublishDeletedOperation_CreatesTombstoneInDatabase()
    {
        var id = Guid.NewGuid();

        await _publisher.PublishAsync(id, "some-payload", version: 1, operation: OperationType.Created);
        await _publisher.PublishAsync(id, string.Empty, version: 2, operation: OperationType.Deleted);

        await Task.Delay(TimeSpan.FromSeconds(5));

        var rows = await DbPoller.WaitForRowsAsync(
            HarnessAATCollectionFixture.ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().ContainSingle();
        rows[0].SourceDeletedAtUtc.Should().NotBeNull();
    }
}
