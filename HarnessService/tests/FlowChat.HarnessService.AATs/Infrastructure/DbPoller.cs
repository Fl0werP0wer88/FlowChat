using FlowChat.HarnessService.Persistence;
using FlowChat.HarnessService.Persistence.Entities.Projections;
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

    private static AppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDbContext(options);
    }
}
