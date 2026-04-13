using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Silverback;
using Silverback.Storage;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.UnitOfWork;

namespace FlowChat.AuthService.UnitTests;

public sealed class SilverbackEfUnitOfWorkTests
{
    [Fact]
    public async Task ExecuteInTransactionAsync_EnlistsAndClearsStorageTransaction_OnSuccess()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var silverbackContext = new TestSilverbackContext();
        await using var dbContext = CreateDbContext(connection);
        var unitOfWork = new SilverbackEfUnitOfWork(dbContext, silverbackContext);

        var result = await unitOfWork.ExecuteInTransactionAsync(
            token =>
            {
                token.ThrowIfCancellationRequested();

                var storageTransaction = silverbackContext.GetStorageTransaction();

                storageTransaction.Should().NotBeNull();
                storageTransaction!.UnderlyingTransaction.Should().NotBeNull();

                return Task.FromResult(42);
            },
            CancellationToken.None);

        result.Should().Be(42);
        silverbackContext.GetStorageTransaction().Should().BeNull();
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ClearsStorageTransaction_OnFailure()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var silverbackContext = new TestSilverbackContext();
        await using var dbContext = CreateDbContext(connection);
        var unitOfWork = new SilverbackEfUnitOfWork(dbContext, silverbackContext);

        var act = () => unitOfWork.ExecuteInTransactionAsync<int>(
            token =>
            {
                token.ThrowIfCancellationRequested();

                silverbackContext.GetStorageTransaction().Should().NotBeNull();

                throw new InvalidOperationException("boom");
            },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("boom");

        silverbackContext.GetStorageTransaction().Should().BeNull();
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new AppDbContext(options);
        dbContext.Database.EnsureCreated();

        return dbContext;
    }

    private sealed class TestSilverbackContext : ISilverbackContext
    {
        private readonly Dictionary<Guid, object> _objects = [];

        public IServiceProvider ServiceProvider => EmptyServiceProvider.Instance;

        public void AddObject(Guid objectTypeId, object obj)
        {
            if (_objects.TryGetValue(objectTypeId, out var existing) && !ReferenceEquals(existing, obj))
            {
                throw new InvalidOperationException($"An object of type {objectTypeId} has already been added.");
            }

            _objects[objectTypeId] = obj;
        }

        public void SetObject(Guid objectTypeId, object obj) => _objects[objectTypeId] = obj;

        public bool RemoveObject(Guid objectTypeId) => _objects.Remove(objectTypeId);

        public T GetObject<T>(Guid objectTypeId) => (T)GetObject(objectTypeId);

        public object GetObject(Guid objectTypeId)
        {
            if (!_objects.TryGetValue(objectTypeId, out var value))
            {
                throw new InvalidOperationException($"The object with type id {objectTypeId} was not found.");
            }

            return value;
        }

        public bool TryGetObject<T>(Guid objectTypeId, [NotNullWhen(true)] out T? obj)
        {
            if (TryGetObject(objectTypeId, out object? value))
            {
                if (value is not T typedValue)
                {
                    throw new InvalidOperationException($"The object with type id {objectTypeId} is not of type {typeof(T)}.");
                }

                obj = typedValue;
                return true;
            }

            obj = default;
            return false;
        }

        public bool TryGetObject(Guid objectTypeId, [NotNullWhen(true)] out object? obj) => _objects.TryGetValue(objectTypeId, out obj);

        public T GetOrAddObject<T>(Guid objectTypeId, Func<T> factory)
        {
            if (TryGetObject(objectTypeId, out T? existing))
            {
                return existing!;
            }

            var created = factory() ?? throw new InvalidOperationException("The factory returned null.");
            AddObject(objectTypeId, created);

            return created;
        }

        public TObject GetOrAddObject<TObject, TArg>(Guid objectTypeId, Func<TArg, TObject> factory, TArg argument)
        {
            if (TryGetObject(objectTypeId, out TObject? existing))
            {
                return existing!;
            }

            var created = factory(argument) ?? throw new InvalidOperationException("The factory returned null.");
            AddObject(objectTypeId, created);

            return created;
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new();

        public object? GetService(Type serviceType) => null;
    }
}
