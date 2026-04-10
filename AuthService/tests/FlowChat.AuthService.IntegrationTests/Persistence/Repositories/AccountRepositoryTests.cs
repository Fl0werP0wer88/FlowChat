using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.Repositories;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.AuthService.IntegrationTests.Persistence.Repositories;

public sealed class AccountRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _dbContext;
    private readonly AccountRepository _sut;

    public AccountRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseOpenIddict()
            .Options;

        _dbContext = new AppDbContext(options);
        _dbContext.Database.EnsureCreated();
        _sut = new AccountRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateAsync_AndGetByEmailAsync_PersistsAndReturnsAccount()
    {
        var emailAddress = EmailAddress.Create("flower@example.com");
        var account = Account.Create("flower", emailAddress, "hash", "stamp");

        await _sut.CreateAsync(account, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByEmailAsync(emailAddress, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Value.Should().Be(account.Id.Value);
        result.FriendlyUserId.Value.Should().Be("flower");
        result.Email.Should().Be(emailAddress);
    }

    [Fact]
    public async Task GetByLoginAsync_WithFriendlyUserId_ReturnsMatchingAccount()
    {
        var account = Account.Create("flower", EmailAddress.Create("flower@example.com"), "hash", "stamp");

        await _sut.CreateAsync(account, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByLoginAsync("FLOWER", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Value.Should().Be(account.Id.Value);
    }

    [Fact]
    public async Task UpdateAsync_PersistsMutableFields()
    {
        var account = Account.Create("flower", EmailAddress.Create("flower@example.com"), "hash", "stamp");

        await _sut.CreateAsync(account, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        account.RecordFailedLogin();
        account.ConfirmEmail();
        account.RotateSecurityStamp("new-stamp");

        await _sut.UpdateAsync(account, CancellationToken.None);
        await _dbContext.SaveChangesAsync();

        var persistedEntity = await _dbContext.Accounts.SingleAsync(x => x.Id == account.Id);

        persistedEntity.AccessFailedCount.Should().Be(1);
        persistedEntity.IsEmailConfirmed.Should().BeTrue();
        persistedEntity.SecurityStamp.Should().Be("new-stamp");
    }
}
