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
    public async Task GetByFriendlyUserIdAsync_WhenFriendlyUserIdHasDifferentCasing_ReturnsProjectedProfile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                "Jdoe",
                EmailAddress.Create("john@example.com"),
                PhoneNumber.Create("+48123123123"));

            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.GetByFriendlyUserIdAsync("  JDOE  ", CancellationToken.None);

        result.Should().NotBeNull();
        result!.FriendlyUserId.Should().Be("jdoe");
        result.Emails.Should().ContainSingle(email => email.IsMain && email.Address == "john@example.com");
        result.Phones.Should().ContainSingle(phone => phone.Number == "+48123123123");
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveProfilesOrderedByDerivedLabelThenFriendlyUserId()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfiles.Add(UserProfile.Create(
                "zoe",
                EmailAddress.Create("zoe@example.com"),
                firstName: "Alex"));
            seedContext.UserProfiles.Add(UserProfile.Create(
                "adam",
                EmailAddress.Create("adam@example.com"),
                firstName: "Alex"));
            seedContext.UserProfiles.Add(UserProfile.Create(
                "hidden",
                EmailAddress.Create("hidden@example.com"),
                isActive: false));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.GetActiveAsync(CancellationToken.None);

        result.Select(profile => profile.FriendlyUserId).Should().Equal("adam", "zoe");
    }

    [Fact]
    public async Task FriendlyUserIdExistsAsync_WhenExcludedUserMatchesFoundProfile_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Guid userId;
        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                "jdoe",
                EmailAddress.Create("john@example.com"));
            userId = profile.Id.Value;

            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.FriendlyUserIdExistsAsync("  JDOE ", userId, CancellationToken.None);

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
