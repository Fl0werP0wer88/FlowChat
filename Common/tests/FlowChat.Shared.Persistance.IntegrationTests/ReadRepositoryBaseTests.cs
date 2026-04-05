using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace FlowChat.Shared.Persistance.IntegrationTests;

public sealed class ReadRepositoryBaseTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _dbContext;
    private readonly TestItemReadRepository _repository;

    public ReadRepositoryBaseTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new TestItemReadRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsDto()
    {
        var entity = new TestItem("Alpha");
        _dbContext.TestItems.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(entity.Id.Value);

        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id.Value);
        result.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task GetByIdAsync_MissingEntity_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_MultipleEntities_ReturnsAll()
    {
        _dbContext.TestItems.AddRange(new TestItem("One"), new TestItem("Two"), new TestItem("Three"));
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
        result.Select(x => x.Name).Should().BeEquivalentTo(["One", "Two", "Three"]);
    }

    // ── Test doubles ────────────────────────────────────────────────────────

    private sealed class TestItem : EntityBase<TestItem>
    {
        public string Name { get; private set; }

        public TestItem(string name) : base(Id<TestItem>.New())
        {
            Name = name;
        }

        // Required by EF Core
        private TestItem() : base(Id<TestItem>.New())
        {
            Name = string.Empty;
        }
    }

    private sealed record TestItemDto(Guid Id, string Name);

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestItem> TestItems => Set<TestItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var utcDateTimeOffsetConverter = new ValueConverter<UtcDateTimeOffset, DateTimeOffset>(
                value => value.Value,
                value => UtcDateTimeOffset.Create(value));

            modelBuilder.Entity<TestItem>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Id)
                    .HasConversion(x => x.Value, x => Id<TestItem>.FromGuid(x));
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.CreatedAtUtc).HasConversion(utcDateTimeOffsetConverter);
                b.Property(x => x.LastModifiedAtUtc).HasConversion(utcDateTimeOffsetConverter);
            });
        }
    }

    private sealed class TestItemReadRepository(DbContext dbContext)
        : ReadRepositoryBase<TestItem, TestItemDto>(dbContext)
    {
        protected override Expression<Func<TestItem, TestItemDto>> MapToDto =>
            x => new TestItemDto(x.Id.Value, x.Name);
    }
}
