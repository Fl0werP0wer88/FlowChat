using FlowChat.HarnessService.AATs.Infrastructure;
using FluentAssertions;

namespace FlowChat.HarnessService.AATs.Features.Projections;

/// <summary>
/// Requires running dev stack (Kafka + PostgreSQL).
/// Run with: dotnet test --filter Category=AAT
/// </summary>
[Collection(HarnessAATCollectionFixture.CollectionName)]
[Trait("Category", "AAT")]
public sealed class ProjectionIsolationAATTests : IAsyncLifetime
{
    // One char over the varchar(100) database constraint - deterministic IsolableException trigger
    private const string OverlongPayload = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private readonly HarnessAATCollectionFixture _fixture;
    private HarnessConsumerHost _consumerHost = null!;
    private KafkaTestPublisher _publisher = null!;

    public ProjectionIsolationAATTests(HarnessAATCollectionFixture fixture)
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
    public async Task BatchWithOneBadItem_GoodItemsProjected_BadItemGoesToDlq()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        await _publisher.PublishAsync(id1, "valid-payload-1", version: 1);
        await _publisher.PublishAsync(id2, OverlongPayload, version: 1);
        await _publisher.PublishAsync(id3, "valid-payload-3", version: 1);

        // Wait for the two good items to be projected (proves the batch was processed
        // and individual retry succeeded for valid items)
        var goodRows = await DbPoller.WaitForRowsAsync(
            HarnessAATCollectionFixture.ConnectionString,
            [id1, id3],
            timeout: TimeSpan.FromSeconds(60));

        goodRows.Should().HaveCount(2);
        goodRows.Should().Contain(r => r.Id == id1 && r.Payload == "valid-payload-1");
        goodRows.Should().Contain(r => r.Id == id3 && r.Payload == "valid-payload-3");

        var badItemInDb = await DbPoller.RowExistsAsync(
            HarnessAATCollectionFixture.ConnectionString,
            id2);
        badItemInDb.Should().BeFalse("the overlong payload violates varchar(100) and must be routed to DLQ");
    }

    [Fact]
    public async Task StaleVersionEvent_DoesNotOverwriteNewerProjection()
    {
        var id = Guid.NewGuid();

        await _publisher.PublishAsync(id, "v3-payload", version: 3);

        // Wait until version 3 is projected before sending stale version
        await DbPoller.WaitForRowsAsync(
            HarnessAATCollectionFixture.ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        await _publisher.PublishAsync(id, "v1-payload", version: 1);

        // Give the consumer time to process the stale event
        await Task.Delay(TimeSpan.FromSeconds(5));

        var rows = await DbPoller.WaitForRowsAsync(
            HarnessAATCollectionFixture.ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().ContainSingle();
        rows[0].SourceVersion.Should().Be(3, "stale version 1 must not overwrite the already-projected version 3");
        rows[0].Payload.Should().Be("v3-payload");
    }
}
