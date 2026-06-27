using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.ProjectionBulk;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.Shared.Persistance.UnitTests.ProjectionBulk;

public sealed class ProjectionBulkRepositoryTests
{
    [Fact]
    public async Task BulkUpsertOrSoftDeleteAsync_EmptyItems_DoesNotCreateEntities()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new TestDbContext(options);
        var entityFactory = new TestProjectionBulkEntityFactory();
        var repository = new ProjectionBulkRepository<
            TestDbContext,
            TestProjectionItem,
            TestProjectionValue,
            TestProjectionEntity,
            TestProjectionBulkEntityFactory>(
                dbContext,
                entityFactory);

        await repository.BulkUpsertOrSoftDeleteAsync([], CancellationToken.None);

        dbContext.ProjectionEntities.Should().BeEmpty();
        entityFactory.CreateUpsertEntityCalls.Should().Be(0);
        entityFactory.CreateTombstoneEntityCalls.Should().Be(0);
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestProjectionEntity> ProjectionEntities => Set<TestProjectionEntity>();
    }

    private sealed class TestProjectionEntity : ReadModelEntityBase
    {
        public Guid Id { get; set; }
    }

    private sealed record TestProjectionValue(Guid Id);

    private sealed record TestProjectionItem(
        TestProjectionValue? Value,
        int SourceVersion,
        DateTimeOffset SourceCreatedAtUtc,
        DateTimeOffset SourceLastModifiedAtUtc,
        DateTimeOffset? SourceDeletedAtUtc)
        : IProjectionCommandItem<TestProjectionValue>;

    private sealed class TestProjectionBulkEntityFactory
        : IProjectionBulkEntityFactory<TestProjectionItem, TestProjectionValue, TestProjectionEntity>
    {
        public int CreateUpsertEntityCalls { get; private set; }

        public int CreateTombstoneEntityCalls { get; private set; }

        public IReadOnlyList<string> UpdateByProperties { get; } = [nameof(TestProjectionEntity.Id)];

        public TestProjectionEntity CreateUpsertEntity(
            TestProjectionValue value,
            int sourceVersion,
            DateTimeOffset sourceCreatedAtUtc,
            DateTimeOffset sourceLastModifiedAtUtc,
            DateTimeOffset? sourceDeletedAtUtc)
        {
            CreateUpsertEntityCalls++;

            return new TestProjectionEntity
            {
                Id = value.Id,
                SourceVersion = sourceVersion,
                SourceCreatedAtUtc = sourceCreatedAtUtc,
                SourceLastModifiedAtUtc = sourceLastModifiedAtUtc,
                SourceDeletedAtUtc = sourceDeletedAtUtc
            };
        }

        public TestProjectionEntity CreateTombstoneEntity(
            TestProjectionItem item,
            DateTimeOffset now)
        {
            CreateTombstoneEntityCalls++;

            return new TestProjectionEntity
            {
                SourceVersion = item.SourceVersion,
                SourceCreatedAtUtc = item.SourceCreatedAtUtc,
                SourceLastModifiedAtUtc = item.SourceLastModifiedAtUtc,
                SourceDeletedAtUtc = item.SourceDeletedAtUtc ?? now
            };
        }
    }
}
