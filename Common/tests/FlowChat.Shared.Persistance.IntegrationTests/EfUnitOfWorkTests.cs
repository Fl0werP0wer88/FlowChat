using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance.IntegrationTests;

public sealed class EfUnitOfWorkTests
{
    [Fact]
    public async Task ExecuteCommandInTransactionAsync_WhenOperationReturnsFailure_RollsBackAndClearsTracker()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<TestDbContext>(dbContext);

        var result = await unitOfWork.ExecuteCommandInTransactionAsync(
            async token =>
            {
                await dbContext.Records.AddAsync(new TestRecord { Name = "discarded" }, token);
                return FlowChatResult<Guid>.Failure(DomainError.UnExpected("failure"));
            },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        dbContext.ChangeTracker.Entries().Should().BeEmpty();
        (await dbContext.Records.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_WhenOperationThrows_RollsBackClearsTrackerAndRethrows()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<TestDbContext>(dbContext);

        var act = () => unitOfWork.ExecuteInTransactionAsync<Guid>(
            async token =>
            {
                await dbContext.Records.AddAsync(new TestRecord { Name = "discarded" }, token);
                throw new InvalidOperationException("boom");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");
        dbContext.ChangeTracker.Entries().Should().BeEmpty();
        (await dbContext.Records.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ExecuteCommandInTransactionAsync_AfterConcurrencyConflict_ReloadsFreshEntityAndSavesNextAttempt()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var retryDbContext = CreateDbContext(connection);
        await using var competingDbContext = CreateDbContext(connection);
        var unitOfWork = new EfUnitOfWork<TestDbContext>(retryDbContext);

        var record = new TestRecord { Name = "original", Version = 1 };
        retryDbContext.Records.Add(record);
        await retryDbContext.SaveChangesAsync();
        retryDbContext.ChangeTracker.Clear();

        var staleRecord = await retryDbContext.Records.SingleAsync();
        var competingRecord = await competingDbContext.Records.SingleAsync();
        competingRecord.Name = "competing";
        competingRecord.Version++;
        await competingDbContext.SaveChangesAsync();

        var firstAttempt = () => unitOfWork.ExecuteCommandInTransactionAsync(
            _ =>
            {
                staleRecord.Name = "stale update";
                return Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid()));
            },
            CancellationToken.None);

        await firstAttempt.Should().ThrowAsync<DbUpdateConcurrencyException>();
        retryDbContext.ChangeTracker.Entries().Should().BeEmpty();

        var secondAttempt = await unitOfWork.ExecuteCommandInTransactionAsync(
            async token =>
            {
                var freshRecord = await retryDbContext.Records.SingleAsync(token);
                freshRecord.Name.Should().Be("competing");
                freshRecord.Name = "retried";
                freshRecord.Version++;
                return FlowChatResult<Guid>.Success(Guid.NewGuid());
            },
            CancellationToken.None);

        secondAttempt.IsSuccess.Should().BeTrue();
        retryDbContext.ChangeTracker.Clear();
        (await retryDbContext.Records.SingleAsync()).Name.Should().Be("retried");
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static TestDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;
        var dbContext = new TestDbContext(options);
        dbContext.Database.EnsureCreated();
        return dbContext;
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestRecord> Records => Set<TestRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestRecord>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Version).IsConcurrencyToken();
            });
        }
    }

    private sealed class TestRecord
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Version { get; set; }
    }
}
