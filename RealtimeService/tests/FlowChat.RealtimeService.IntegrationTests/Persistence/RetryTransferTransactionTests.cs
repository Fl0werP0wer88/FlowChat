using FlowChat.RealtimeService.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Producing.TransactionalOutbox;

namespace FlowChat.RealtimeService.IntegrationTests.Persistence;

public sealed class RetryTransferTransactionTests
{
    [Fact]
    public async Task CommitAsync_OutboxAndOffset_ArePersistedTogether()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        AddRetryTransfer(fixture.Context);

        await fixture.Context.SaveChangesAsync();
        await transaction.CommitAsync();
        fixture.Context.ChangeTracker.Clear();

        (await fixture.Context.SilverbackOutboxMessages.CountAsync()).Should().Be(1);
        (await fixture.Context.SilverbackStoredOffsets.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RollbackAsync_FailureBeforeCommit_PersistsNeitherOutboxNorOffset()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        await using var transaction = await fixture.Context.Database.BeginTransactionAsync();
        AddRetryTransfer(fixture.Context);

        await fixture.Context.SaveChangesAsync();
        await transaction.RollbackAsync();
        fixture.Context.ChangeTracker.Clear();

        (await fixture.Context.SilverbackOutboxMessages.CountAsync()).Should().Be(0);
        (await fixture.Context.SilverbackStoredOffsets.CountAsync()).Should().Be(0);
    }

    private static void AddRetryTransfer(AppDbContext context)
    {
        var outbox = CreateEntity<SilverbackOutboxMessage>();
        context.Add(outbox);
        context.Entry(outbox).Property(nameof(SilverbackOutboxMessage.Content)).CurrentValue = new byte[] { 1, 2, 3 };
        context.Entry(outbox).Property(nameof(SilverbackOutboxMessage.EndpointName)).CurrentValue = "retry-topic";
        context.Entry(outbox).Property(nameof(SilverbackOutboxMessage.Created)).CurrentValue = DateTime.UtcNow;

        var offset = CreateEntity<SilverbackStoredOffset>();
        context.Add(offset);
        context.Entry(offset).Property(nameof(SilverbackStoredOffset.GroupId)).CurrentValue = "realtime-service";
        context.Entry(offset).Property(nameof(SilverbackStoredOffset.Topic)).CurrentValue = "main-topic";
        context.Entry(offset).Property(nameof(SilverbackStoredOffset.Partition)).CurrentValue = 0;
        context.Entry(offset).Property(nameof(SilverbackStoredOffset.Offset)).CurrentValue = 43L;
    }

    private static T CreateEntity<T>() where T : class =>
        (T)(Activator.CreateInstance(typeof(T), nonPublic: true)
            ?? throw new InvalidOperationException($"Could not create {typeof(T).Name}."));

    private sealed class SqliteFixture(SqliteConnection connection, AppDbContext context) : IAsyncDisposable
    {
        public AppDbContext Context { get; } = context;

        public static async Task<SqliteFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options);
            await context.Database.EnsureCreatedAsync();
            return new SqliteFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
