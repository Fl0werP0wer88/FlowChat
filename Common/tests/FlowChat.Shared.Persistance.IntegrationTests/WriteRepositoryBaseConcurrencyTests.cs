using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlowChat.Shared.Persistance.IntegrationTests;

public sealed class WriteRepositoryBaseConcurrencyTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public WriteRepositoryBaseConcurrencyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentModification_ThrowsDbUpdateConcurrencyException()
    {
        using var context1 = CreateDbContext();
        context1.Database.EnsureCreated();

        var entity = TestAggregate.Create("original");
        context1.TestAggregates.Add(entity);
        await context1.SaveChangesAsync();

        var entityId = Id<TestAggregate>.FromGuid(entity.Id.Value);

        using var context2 = CreateDbContext();

        var entityInContext1 = await context1.TestAggregates
            .FirstAsync(x => x.Id == entityId);
        var entityInContext2 = await context2.TestAggregates
            .FirstAsync(x => x.Id == entityId);

        entityInContext1.Rename("from-context-1");
        await context1.SaveChangesAsync();

        entityInContext2.Rename("from-context-2");

        var act = () => context2.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task UpdateAsync_SequentialModifications_SucceedsAndIncrementsVersion()
    {
        using var context = CreateDbContext();
        context.Database.EnsureCreated();

        var entity = TestAggregate.Create("v1");
        context.TestAggregates.Add(entity);
        await context.SaveChangesAsync();

        entity.Version.Should().Be(1);

        entity.Rename("v2");
        await context.SaveChangesAsync();

        entity.Version.Should().Be(2);

        entity.Rename("v3");
        await context.SaveChangesAsync();

        entity.Version.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_VersionPersistedToDatabase_ReloadsCorrectly()
    {
        using var context1 = CreateDbContext();
        context1.Database.EnsureCreated();

        var entity = TestAggregate.Create("initial");
        context1.TestAggregates.Add(entity);
        await context1.SaveChangesAsync();

        entity.Rename("updated");
        await context1.SaveChangesAsync();

        var entityId = Id<TestAggregate>.FromGuid(entity.Id.Value);

        using var context2 = CreateDbContext();
        var reloaded = await context2.TestAggregates
            .FirstAsync(x => x.Id == entityId);

        reloaded.Version.Should().Be(2);
    }

    private TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var utcDateTimeOffsetConverter = new ValueConverter<UtcDateTimeOffset, DateTimeOffset>(
                value => value.Value,
                value => UtcDateTimeOffset.Create(value));

            modelBuilder.Entity<TestAggregate>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id)
                    .HasConversion(x => x.Value, x => Id<TestAggregate>.FromGuid(x));
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Version).IsConcurrencyToken();
                b.Property(x => x.CreatedAtUtc).HasConversion(utcDateTimeOffsetConverter);
                b.Property(x => x.LastModifiedAtUtc).HasConversion(utcDateTimeOffsetConverter);
            });
        }
    }

    private sealed class TestAggregate : AggregateRootBase<TestAggregate>, IAggregateRoot
    {
        public string Name { get; private set; } = string.Empty;

        private TestAggregate() : base(Id<TestAggregate>.New()) { }

        private TestAggregate(string name) : base(Id<TestAggregate>.New())
        {
            Name = name;
        }

        public static TestAggregate Create(string name) => new(name);

        public void Rename(string name)
        {
            Name = name;
        }
    }
}
