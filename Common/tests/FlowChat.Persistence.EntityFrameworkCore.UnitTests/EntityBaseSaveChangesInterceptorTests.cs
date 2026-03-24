using FlowChat.Domain.Abstractions;
using FlowChat.Persistence.EntityFrameworkCore.Auditing;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Persistence.EntityFrameworkCore.UnitTests;

public sealed class EntityBaseSaveChangesInterceptorTests
{
    [Fact]
    public async Task SavingChanges_ForAddedEntity_SetsAllAuditFields()
    {
        var entity = TestEntity.Create("added");

        await using var dbContext = CreateDbContext();
        dbContext.TestEntities.Add(entity);

        await dbContext.SaveChangesAsync();

        Assert.Equal("system", entity.CreatedBy);
        Assert.Equal("system", entity.LastModifiedBy);
        Assert.NotEqual(default, entity.CreatedAtUtc);
        Assert.Equal(entity.CreatedAtUtc, entity.LastModifiedAtUtc);
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

        Assert.Equal(createdBy, entity.CreatedBy);
        Assert.Equal(createdAtUtc, entity.CreatedAtUtc);
        Assert.Equal("system", entity.LastModifiedBy);
        Assert.True(entity.LastModifiedAtUtc >= createdAtUtc);
    }

    [Fact]
    public async Task SavingChanges_IgnoresEntitiesThatDoNotDeriveFromEntityBase()
    {
        await using var dbContext = CreateDbContext();

        dbContext.PlainEntities.Add(new PlainEntity { Name = "plain" });

        await dbContext.SaveChangesAsync();

        Assert.Equal(1, await dbContext.PlainEntities.CountAsync());
    }

    private static TestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestEntity> TestEntities => Set<TestEntity>();
        public DbSet<PlainEntity> PlainEntities => Set<PlainEntity>();
    }

    private sealed class TestEntity : EntityBase<TestEntity>
    {
        private TestEntity()
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
