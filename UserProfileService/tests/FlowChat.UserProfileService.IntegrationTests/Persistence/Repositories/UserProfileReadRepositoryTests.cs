using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.IntegrationTests.Persistence.Repositories;

public sealed class UserProfileReadRepositoryTests
{
    [Fact]
    public async Task GetByUserNameAsync_WhenUserNameHasDifferentCasing_ReturnsProjectedProfile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                "Jdoe",
                "John Doe",
                EmailAddress.Create("john@example.com"),
                PhoneNumber.Create("+48123123123"));

            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.GetByUserNameAsync("  JDOE  ", CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserName.Should().Be("Jdoe");
        result.Emails.Should().ContainSingle(email => email.IsMain && email.Address == "john@example.com");
        result.Phones.Should().ContainSingle(phone => phone.Number == "+48123123123");
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveProfilesOrderedByDisplayNameThenUserName()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfiles.Add(UserProfile.Create(
                "zoe",
                "Alex",
                EmailAddress.Create("zoe@example.com")));
            seedContext.UserProfiles.Add(UserProfile.Create(
                "adam",
                "Alex",
                EmailAddress.Create("adam@example.com")));
            seedContext.UserProfiles.Add(UserProfile.Create(
                "hidden",
                "Hidden User",
                EmailAddress.Create("hidden@example.com"),
                isActive: false));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.GetActiveAsync(CancellationToken.None);

        result.Select(profile => profile.UserName).Should().Equal("adam", "zoe");
    }

    [Fact]
    public async Task UserNameExistsAsync_WhenExcludedUserMatchesFoundProfile_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Guid userId;
        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                "jdoe",
                "John Doe",
                EmailAddress.Create("john@example.com"));
            userId = profile.Id.Value;

            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.UserNameExistsAsync("  JDOE ", userId, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EmailAddressExistsAsync_WhenEmailIsPersisted_ReturnsTrue()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                "jdoe",
                "John Doe",
                EmailAddress.Create("john@example.com"));

            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.EmailAddressExistsAsync(" john@example.com ", CancellationToken.None);

        result.Should().BeTrue();
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
