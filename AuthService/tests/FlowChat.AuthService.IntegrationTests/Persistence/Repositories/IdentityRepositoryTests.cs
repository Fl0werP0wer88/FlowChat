using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.Identity;
using FlowChat.AuthService.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.IntegrationTests.Persistence.Repositories;

public sealed class IdentityRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly UserManager<UserEntity> _userManager;
    private readonly IdentityRepository _sut;

    public IdentityRepositoryTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(_connection));

        services.AddIdentityCore<UserEntity>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
            })
            .AddRoles<RoleEntity>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddDataProtection();
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.EnsureCreated();

        _userManager = _serviceProvider.GetRequiredService<UserManager<UserEntity>>();
        _sut = new IdentityRepository(_userManager);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateUserAsync_ValidIdentity_PersistsUserAndReturnsId()
    {
        var identity = Identity.Create(Guid.NewGuid(), "newuser", "newuser@test.com");

        var result = await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        result.Should().Be(identity.Id.Value);
        var createdUser = await _userManager.FindByIdAsync(result.ToString());
        createdUser.Should().NotBeNull();
        createdUser!.UserName.Should().Be("newuser");
        createdUser.Email.Should().Be("newuser@test.com");
        createdUser.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task CreateUserAsync_DuplicateUserName_ThrowsInvalidOperationException()
    {
        var firstIdentity = Identity.Create(Guid.NewGuid(), "duplicate", "one@test.com");
        var secondIdentity = Identity.Create(Guid.NewGuid(), "duplicate", "two@test.com");

        await _sut.CreateUserAsync(firstIdentity, "Pass1234!", CancellationToken.None);

        var act = () => _sut.CreateUserAsync(secondIdentity, "Pass1234!", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User creation failed:*");
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingUser_ReturnsMappedDomainIdentity()
    {
        var identity = Identity.Create(Guid.NewGuid(), "lookup-user", "lookup@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var result = await _sut.GetByEmailAsync("lookup@test.com", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Value.Should().Be(identity.Id.Value);
        result.UserName.Should().Be("lookup-user");
        result.Email.Should().Be("lookup@test.com");
        result.EmailConfirmed.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ExistingUser_PersistsChangedConfirmationFlags()
    {
        var identity = Identity.Create(Guid.NewGuid(), "updatable", "updatable@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var updatedIdentity = Identity.Restore(
            identity.Id.Value,
            "updatable",
            "updatable@test.com",
            null,
            emailConfirmed: true,
            phoneNumberConfirmed: false);

        await _sut.UpdateAsync(updatedIdentity, CancellationToken.None);

        var persistedUser = await _userManager.FindByIdAsync(identity.Id.Value.ToString());
        persistedUser.Should().NotBeNull();
        persistedUser!.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateUserAsync_ConfirmedUserWithValidEmail_ReturnsAuthenticatedUser()
    {
        var identity = Identity.Create(Guid.NewGuid(), "flower", "flower@example.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);
        await ConfirmEmailAsync(identity);

        var result = await _sut.AuthenticateUserAsync("flower@example.com", "Pass1234!", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(identity.Id.Value);
        result.UserName.Should().Be("flower");
        result.Email.Should().Be("flower@example.com");
    }

    [Fact]
    public async Task AuthenticateUserAsync_ConfirmedUserWithValidUserName_ReturnsAuthenticatedUser()
    {
        var identity = Identity.Create(Guid.NewGuid(), "flower", "flower@example.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);
        await ConfirmEmailAsync(identity);

        var result = await _sut.AuthenticateUserAsync("flower", "Pass1234!", CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserName.Should().Be("flower");
    }

    [Fact]
    public async Task AuthenticateUserAsync_UnconfirmedUser_ReturnsNull()
    {
        var identity = Identity.Create(Guid.NewGuid(), "unconfirmed", "unconfirmed@example.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var result = await _sut.AuthenticateUserAsync("unconfirmed@example.com", "Pass1234!", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateUserAsync_InvalidPassword_ReturnsNull()
    {
        var identity = Identity.Create(Guid.NewGuid(), "wrongpw", "wrongpw@example.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);
        await ConfirmEmailAsync(identity);

        var result = await _sut.AuthenticateUserAsync("wrongpw@example.com", "WrongPassword!", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAuthenticatedUserByIdAsync_ConfirmedUser_ReturnsAuthenticatedUser()
    {
        var identity = Identity.Create(Guid.NewGuid(), "authuser", "authuser@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);
        await ConfirmEmailAsync(identity);

        var result = await _sut.GetAuthenticatedUserByIdAsync(identity.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(identity.Id.Value);
        result.UserName.Should().Be("authuser");
        result.Email.Should().Be("authuser@test.com");
    }

    [Fact]
    public async Task GetAuthenticatedUserByIdAsync_UnconfirmedUser_ReturnsNull()
    {
        var identity = Identity.Create(Guid.NewGuid(), "unconfirmed2", "unconfirmed2@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var result = await _sut.GetAuthenticatedUserByIdAsync(identity.Id.Value, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAuthenticatedUserByIdAsync_NonExistentUser_ReturnsNull()
    {
        var result = await _sut.GetAuthenticatedUserByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_ValidData_CanBeRetrieved()
    {
        var identity = Identity.Create(Guid.NewGuid(), "tokenuser", "tokenuser@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var expiresAtUtc = DateTime.UtcNow.AddDays(7);
        await _sut.SaveRefreshTokenAsync(identity.Id.Value, "refresh-token-value", expiresAtUtc, CancellationToken.None);

        var result = await _sut.GetRefreshTokenAsync(identity.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.Token.Should().Be("refresh-token-value");
        result.Value.ExpiresAtUtc.Should().BeCloseTo(expiresAtUtc, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SaveRefreshTokenAsync_CalledTwice_OverwritesPreviousToken()
    {
        var identity = Identity.Create(Guid.NewGuid(), "overwrite", "overwrite@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var expiry = DateTime.UtcNow.AddDays(7);
        await _sut.SaveRefreshTokenAsync(identity.Id.Value, "first-token", expiry, CancellationToken.None);
        await _sut.SaveRefreshTokenAsync(identity.Id.Value, "second-token", expiry, CancellationToken.None);

        var result = await _sut.GetRefreshTokenAsync(identity.Id.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Value.Token.Should().Be("second-token");
    }

    [Fact]
    public async Task GetRefreshTokenAsync_NoTokenSaved_ReturnsNull()
    {
        var identity = Identity.Create(Guid.NewGuid(), "notoken", "notoken@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var result = await _sut.GetRefreshTokenAsync(identity.Id.Value, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRefreshTokenAsync_NonExistentUser_ReturnsNull()
    {
        var result = await _sut.GetRefreshTokenAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_ExistingToken_RemovesToken()
    {
        var identity = Identity.Create(Guid.NewGuid(), "revoke", "revoke@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var expiry = DateTime.UtcNow.AddDays(7);
        await _sut.SaveRefreshTokenAsync(identity.Id.Value, "token-to-revoke", expiry, CancellationToken.None);

        await _sut.RevokeRefreshTokenAsync(identity.Id.Value, CancellationToken.None);

        var result = await _sut.GetRefreshTokenAsync(identity.Id.Value, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_NoToken_DoesNotThrow()
    {
        var identity = Identity.Create(Guid.NewGuid(), "norevoke", "norevoke@test.com");
        await _sut.CreateUserAsync(identity, "Pass1234!", CancellationToken.None);

        var act = () => _sut.RevokeRefreshTokenAsync(identity.Id.Value, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private async Task ConfirmEmailAsync(Identity identity)
    {
        var confirmedIdentity = Identity.Restore(
            identity.Id.Value,
            identity.UserName,
            identity.Email,
            identity.PhoneNumber,
            emailConfirmed: true,
            phoneNumberConfirmed: identity.PhoneNumberConfirmed,
            firstName: identity.FirstName,
            lastName: identity.LastName);

        await _sut.UpdateAsync(confirmedIdentity, CancellationToken.None);
    }
}
