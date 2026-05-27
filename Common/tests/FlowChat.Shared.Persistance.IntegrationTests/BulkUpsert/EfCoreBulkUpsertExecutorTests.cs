using FlowChat.Shared.Application;
using FlowChat.Shared.Persistance.BulkUpsert;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Shared.Persistance.IntegrationTests.BulkUpsert;

public sealed class EfCoreBulkUpsertExecutorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _dbContext;
    private readonly EfCoreBulkUpsertExecutor<TestDbContext, TestBulkItem> _executor;

    public EfCoreBulkUpsertExecutorTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new TestDbContext(options);
        _dbContext.Database.EnsureCreated();

        _executor = new EfCoreBulkUpsertExecutor<TestDbContext, TestBulkItem>(
            _dbContext,
            new EfCoreBulkUpsertOptions<TestBulkItem>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task UpsertAsync_WhenItemDoesNotExist_InsertsItem()
    {
        var item = new TestBulkItem
        {
            Id = Guid.NewGuid(),
            Name = "Alpha"
        };

        var result = await _executor.UpsertAsync([item], CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RequestedCount.Should().Be(1);
        result.Value.UpsertedCount.Should().Be(1);

        var saved = await _dbContext.TestBulkItems.SingleAsync();
        saved.Id.Should().Be(item.Id);
        saved.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task UpsertAsync_WhenItemExists_UpdatesItem()
    {
        var id = Guid.NewGuid();
        _dbContext.TestBulkItems.Add(new TestBulkItem
        {
            Id = id,
            Name = "Before"
        });
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var result = await _executor.UpsertAsync(
            [
                new TestBulkItem
                {
                    Id = id,
                    Name = "After"
                }
            ],
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var saved = await _dbContext.TestBulkItems.SingleAsync();
        saved.Id.Should().Be(id);
        saved.Name.Should().Be("After");
    }

    [Fact]
    public void AddEfCoreBulkUpsertExecutor_RegistersExecutorForItem()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(options => options.UseSqlite(_connection));
        services.AddEfCoreBulkUpsertExecutor<TestDbContext, TestBulkItem>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var executor = scope.ServiceProvider.GetRequiredService<IBulkUpsertExecutor<TestBulkItem>>();

        executor.Should().BeOfType<EfCoreBulkUpsertExecutor<TestDbContext, TestBulkItem>>();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<TestBulkItem> TestBulkItems => Set<TestBulkItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestBulkItem>(builder =>
            {
                builder.HasKey(x => x.Id);
                builder.Property(x => x.Name).IsRequired();
            });
        }
    }

    private sealed class TestBulkItem
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
