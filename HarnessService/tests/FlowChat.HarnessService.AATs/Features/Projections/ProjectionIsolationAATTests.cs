using FlowChat.Core.Messaging;
using FlowChat.HarnessService.AATs.Infrastructure;
using FluentAssertions;

namespace FlowChat.HarnessService.AATs.Features.Projections;

/// <summary>
/// Requires running dev stack (Kafka + PostgreSQL) and both HarnessService API and Consumers.
/// Run with: dotnet test --filter Category=AAT
/// </summary>
[Trait("Category", "AAT")]
public sealed class ProjectionIsolationAATTests : IAsyncLifetime
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=flowchat_harness_db;Username=flowchat_app;Password=flowchat_app_pw;";
    private const string BootstrapServers = "localhost:9092";
    private const string Topic = "test.flowchat.harness.projection.events";
    private const string RetryTopic = "test.flowchat.harness.projection.events.retry";
    private const string DlqTopic = "test.flowchat.harness.projection.events.dlq";
    private const string ApiBaseUrl = "https://localhost:7300";
    private const string ApiKey = "FLOWCHAT_DEVELOPMENT_INTERNAL_API_KEY_CHANGE_ME";

    // One char over the varchar(100) database constraint — deterministic IsolableException trigger
    private const string OverlongPayload = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    private HarnessConsumerHost _consumerHost = null!;
    private KafkaTestPublisher _publisher = null!;

    public async Task InitializeAsync()
    {
        _consumerHost = new HarnessConsumerHost(ApiBaseUrl, ApiKey, BootstrapServers, Topic, RetryTopic, DlqTopic);
        await _consumerHost.InitializeAsync();
        _publisher = new KafkaTestPublisher(BootstrapServers, Topic);
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
            ConnectionString,
            [id1, id3],
            timeout: TimeSpan.FromSeconds(60));

        goodRows.Should().HaveCount(2);
        goodRows.Should().Contain(r => r.Id == id1 && r.Payload == "valid-payload-1");
        goodRows.Should().Contain(r => r.Id == id3 && r.Payload == "valid-payload-3");

        var badItemInDb = await DbPoller.RowExistsAsync(ConnectionString, id2);
        badItemInDb.Should().BeFalse("the overlong payload violates varchar(100) and must be routed to DLQ");
    }

    [Fact]
    public async Task StaleVersionEvent_DoesNotOverwriteNewerProjection()
    {
        var id = Guid.NewGuid();

        await _publisher.PublishAsync(id, "v3-payload", version: 3);

        // Wait until version 3 is projected before sending stale version
        await DbPoller.WaitForRowsAsync(ConnectionString, [id], timeout: TimeSpan.FromSeconds(30));

        await _publisher.PublishAsync(id, "v1-payload", version: 1);

        // Give the consumer time to process the stale event
        await Task.Delay(TimeSpan.FromSeconds(5));

        var rows = await DbPoller.WaitForRowsAsync(
            ConnectionString,
            [id],
            timeout: TimeSpan.FromSeconds(30));

        rows.Should().ContainSingle();
        rows[0].SourceVersion.Should().Be(3, "stale version 1 must not overwrite the already-projected version 3");
        rows[0].Payload.Should().Be("v3-payload");
    }
}
