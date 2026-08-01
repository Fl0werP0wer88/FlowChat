using FlowChat.HarnessService.Persistence;
using FlowChat.HarnessService.Persistence.Entities.Projections;
using FlowChat.HarnessService.Persistence.Entities.Retry;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public static class DbPoller
{
    public static async Task<IList<ProjectionTestEntity>> WaitForRowsAsync(
        string connectionString,
        IReadOnlyCollection<Guid> ids,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var db = CreateDbContext(connectionString);
            var rows = await db.ProjectionTests
                .Where(x => ids.Contains(x.Id))
                .ToListAsync(cancellationToken);

            if (rows.Count >= ids.Count)
                return rows;

            await Task.Delay(200, cancellationToken);
        }

        throw new TimeoutException(
            $"Timed out after {timeout} waiting for {ids.Count} projection rows.");
    }

    public static async Task<bool> RowExistsAsync(
        string connectionString,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateDbContext(connectionString);
        return await db.ProjectionTests.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public static async Task<ProjectionTestEntity> WaitForRowAsync(
        string connectionString,
        Guid id,
        Func<ProjectionTestEntity, bool> predicate,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var db = CreateDbContext(connectionString);
            var row = await db.ProjectionTests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (row is not null && predicate(row))
                return row;

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"Timed out after {timeout} waiting for projection row {id}.");
    }

    public static async Task WaitForStoredOffsetAsync(
        string connectionString,
        string groupId,
        string topic,
        long minimumOffset,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var db = CreateDbContext(connectionString);
            var reachedOffset = await db.SilverbackStoredOffsets.AnyAsync(
                x => x.GroupId == groupId && x.Topic == topic && x.Offset >= minimumOffset,
                cancellationToken);
            if (reachedOffset)
                return;

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"Timed out after {timeout} waiting for stored offset {minimumOffset}.");
    }

    public static async Task<long> GetStoredOffsetAsync(
        string connectionString,
        string groupId,
        string topic,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateDbContext(connectionString);
        var offsets = await db.SilverbackStoredOffsets
            .Where(x => x.GroupId == groupId && x.Topic == topic)
            .Select(x => x.Offset)
            .ToListAsync(cancellationToken);
        return offsets.Count == 0 ? -1 : offsets.Max();
    }

    public static async Task<RetryPipelineTestResultEntity> WaitForRetryResultAsync(
        string connectionString,
        Guid scenarioId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var db = CreateDbContext(connectionString);
            var result = await db.RetryPipelineTestResults
                .SingleOrDefaultAsync(x => x.ScenarioId == scenarioId, cancellationToken);
            if (result is not null)
                return result;

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"Timed out after {timeout} waiting for retry result {scenarioId}.");
    }

    public static async Task<bool> RetryResultExistsAsync(
        string connectionString,
        Guid scenarioId,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateDbContext(connectionString);
        return await db.RetryPipelineTestResults.AnyAsync(
            x => x.ScenarioId == scenarioId,
            cancellationToken);
    }

    public static async Task WaitForAtomicRetryTransferAsync(
        string connectionString,
        string sourceTopic,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        var lastHasOutboxMessage = false;
        var lastHasOffset = false;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var db = CreateDbContext(connectionString);
            lastHasOutboxMessage = await db.SilverbackOutboxMessages.AnyAsync(cancellationToken);
            lastHasOffset = await db.SilverbackStoredOffsets.AnyAsync(
                x => x.Topic == sourceTopic,
                cancellationToken);
            if (lastHasOutboxMessage && lastHasOffset)
                return;

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException(
            $"Timed out waiting for the atomic outbox and offset transfer. Outbox: {lastHasOutboxMessage}; offset: {lastHasOffset}.");
    }

    private static AppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDbContext(options);
    }
}
