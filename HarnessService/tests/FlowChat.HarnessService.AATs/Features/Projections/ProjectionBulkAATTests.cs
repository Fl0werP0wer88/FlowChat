using FlowChat.HarnessService.AATs.Infrastructure;
using FluentAssertions;

namespace FlowChat.HarnessService.AATs.Features.Projections;

/// <summary>
/// Uses Kafka and PostgreSQL Testcontainers.
/// Run with: dotnet test --filter Category=AAT
/// </summary>
[Collection(HarnessAATCollectionFixture.CollectionName)]
[Trait("Category", "AAT")]
public sealed class ProjectionBulkAATTests : IAsyncLifetime
{
    private HarnessConsumerHost _consumerHost = null!;
    private KafkaTestPublisher _publisher = null!;
    private readonly HarnessAATCollectionFixture _fixture;

    public ProjectionBulkAATTests(HarnessAATCollectionFixture fixture)
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
    public async Task ValidEvents_WhenConsumed_AllProjectionsAreStored()
    {
        var expected = new Dictionary<Guid, string>
        {
            [Guid.NewGuid()] = "valid-payload-1",
            [Guid.NewGuid()] = "valid-payload-2",
            [Guid.NewGuid()] = "valid-payload-3"
        };

        foreach (var (id, payload) in expected)
        {
            await _publisher.PublishAsync(id, payload, version: 1);
        }

        var rows = await DbPoller.WaitForRowsAsync(
            _fixture.ConnectionString,
            expected.Keys,
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().HaveCount(expected.Count);
        rows.Should().OnlyContain(row =>
            expected.ContainsKey(row.Id) &&
            expected[row.Id] == row.Payload &&
            row.SourceVersion == 1);
    }

    [Fact]
    public async Task StaleVersionEvent_WhenConsumed_DoesNotOverwriteNewerProjection()
    {
        var id = Guid.NewGuid();

        await _publisher.PublishAsync(id, "v3-payload", version: 3);

        await DbPoller.WaitForRowsAsync(
            _fixture.ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        var offsetBeforeStaleEvent = await DbPoller.GetStoredOffsetAsync(
            _fixture.ConnectionString,
            _consumerHost.ProjectionMainGroupId,
            _fixture.Projection.Topic);

        await _publisher.PublishAsync(id, "v1-payload", version: 1);

        await DbPoller.WaitForStoredOffsetAsync(
            _fixture.ConnectionString,
            _consumerHost.ProjectionMainGroupId,
            _fixture.Projection.Topic,
            minimumOffset: offsetBeforeStaleEvent + 1,
            timeout: TimeSpan.FromSeconds(30));

        var rows = await DbPoller.WaitForRowsAsync(
            _fixture.ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().ContainSingle();
        rows[0].SourceVersion.Should().Be(3);
        rows[0].Payload.Should().Be("v3-payload");
    }
}
