using AutoFixture;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FlowChat.Shared.Persistance.IntegrationTests;

public sealed class EntityBaseSaveChangesInterceptorTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task SavingChanges_ForAddedEntity_SetsAllAuditFields()
    {
        var entity = TestEntity.Create("added");

        await using var dbContext = CreateDbContext();
        dbContext.TestEntities.Add(entity);

        await dbContext.SaveChangesAsync();

        entity.CreatedBy.Should().Be("system");
        entity.LastModifiedBy.Should().Be("system");
        entity.CreatedAtUtc.Should().NotBeNull();
        entity.LastModifiedAtUtc.Value.Should().BeOnOrAfter(entity.CreatedAtUtc.Value);
    }

    [Fact]
    public async Task SavingChanges_ForModifiedEntity_UpdatesOnlyLastModifiedFields()
    {
        await using var dbContext = CreateDbContext();
        var entity = TestEntity.Create("before");
        dbContext.TestEntities.Add(entity);
        await dbContext.SaveChangesAsync();

        var createdBy = entity.CreatedBy;
        var createdAtUtc = entity.CreatedAtUtc;

        entity.Rename("after");
        await dbContext.SaveChangesAsync();

        entity.CreatedBy.Should().Be(createdBy);
        entity.CreatedAtUtc.Should().Be(createdAtUtc);
        entity.LastModifiedBy.Should().Be("system");
        entity.LastModifiedAtUtc.Value.Should().BeOnOrAfter(createdAtUtc.Value);
    }

    [Fact]
    public async Task SavingChanges_ForAddedEntity_KeepsVersionAtInitialValue()
    {
        var entity = TestEntity.Create("new");

        await using var dbContext = CreateDbContext();
        dbContext.TestEntities.Add(entity);

        await dbContext.SaveChangesAsync();

        entity.Version.Should().Be(1);
    }

    [Fact]
    public async Task SavingChanges_ForModifiedEntity_IncrementsVersion()
    {
        await using var dbContext = CreateDbContext();
        var entity = TestEntity.Create("before");
        dbContext.TestEntities.Add(entity);
        await dbContext.SaveChangesAsync();

        entity.Version.Should().Be(1);

        entity.Rename("after");
        await dbContext.SaveChangesAsync();

        entity.Version.Should().Be(2);
    }

    [Fact]
    public async Task SavingChanges_ForMultipleModifications_IncrementsVersionEachTime()
    {
        await using var dbContext = CreateDbContext();
        var entity = TestEntity.Create("v1");
        dbContext.TestEntities.Add(entity);
        await dbContext.SaveChangesAsync();

        entity.Rename("v2");
        await dbContext.SaveChangesAsync();

        entity.Rename("v3");
        await dbContext.SaveChangesAsync();

        entity.Version.Should().Be(3);
    }

    [Fact]
    public async Task SavingChanges_IgnoresEntitiesThatDoNotDeriveFromEntityBase()
    {
        await using var dbContext = CreateDbContext();

        dbContext.PlainEntities.Add(new PlainEntity { Name = "plain" });

        await dbContext.SaveChangesAsync();

        (await dbContext.PlainEntities.CountAsync()).Should().Be(1);
    }

    private TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(_fixture.Create<Guid>().ToString("N"))
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<PlainEntity> PlainEntities => Set<PlainEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var idConverter = new ValueConverter<Id<TestEntity>, Guid>(
                id => id.Value,
                value => Id<TestEntity>.FromGuid(value));
            var utcDateTimeOffsetConverter = new ValueConverter<UtcDateTimeOffset, DateTimeOffset>(
                value => value.Value,
                value => UtcDateTimeOffset.Create(value));

            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasConversion(idConverter);
                entity.Property(x => x.CreatedAtUtc).HasConversion(utcDateTimeOffsetConverter);
                entity.Property(x => x.LastModifiedAtUtc).HasConversion(utcDateTimeOffsetConverter);
            });
        }
    }

    private sealed class TestEntity : EntityBase<TestEntity>
    {
        private TestEntity()
            : base(Id<TestEntity>.New())
        {
        }

        private TestEntity(string name)
            : base(Id<TestEntity>.New())
        {
            Name = name;
        }

        public string Name { get; private set; } = string.Empty;

        public static TestEntity Create(string name) => new(name);

        public void Rename(string name)
        {
            Name = name;
        }
    }

    private sealed class PlainEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
