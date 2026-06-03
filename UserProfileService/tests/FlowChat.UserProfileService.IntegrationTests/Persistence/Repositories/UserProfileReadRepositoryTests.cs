using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
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
    public async Task SearchAsync_WhenProfilesMatchAllPrefixes_ReturnsMatchingRowsWithPreservedResponseShape()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfiles.AddRange(
                UserProfile.Create(
                    Id<UserProfile>.New(),
                    "jdoe",
                    EmailAddress.Create("jane@example.com"),
                    PhoneNumber.Create("+48123123123"),
                    avatarUrl: "https://cdn.example/jane.png",
                    bio: "about Jane",
                    firstName: "Jane",
                    lastName: "Doe",
                    organization: "FlowChat"),
                UserProfile.Create(
                    Id<UserProfile>.New(),
                    "jdoe2",
                    EmailAddress.Create("janet@example.com"),
                    firstName: "Janet",
                    lastName: "Doe",
                    organization: "FlowLab"),
                UserProfile.Create(
                    Id<UserProfile>.New(),
                    "jwrong",
                    EmailAddress.Create("wrong@example.com"),
                    firstName: "Jane",
                    lastName: "Smith",
                    organization: "FlowChat"));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.SearchAsync("Jan", "Do", "Flow", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(profile => profile.FriendlyUserId).Should().Equal("jdoe", "jdoe2");
        result[0].Id.Should().NotBeEmpty();
        result[0].FirstName.Should().Be("Jane");
        result[0].LastName.Should().Be("Doe");
        result[0].Organization.Should().Be("FlowChat");
        result[0].Emails.Should().ContainSingle(email => email.Address == "jane@example.com"
            && email.IsMain
            && !email.IsConfirmed
            && email.IsVisible);
        result[0].Phones.Should().ContainSingle(phone => phone.Number == "+48123123123"
            && phone.IsMain
            && !phone.IsConfirmed
            && phone.IsVisible);
        result[0].AvatarUrl.Should().Be("https://cdn.example/jane.png");
        result[0].Bio.Should().Be("about Jane");
        result[0].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SearchAsync_WhenCriterionIsNullOrEmpty_IgnoresThatFilter()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.UserProfiles.AddRange(
                UserProfile.Create(
                    Id<UserProfile>.New(),
                    "jdoe",
                    EmailAddress.Create("jane@example.com"),
                    firstName: "Jane",
                    lastName: "Doe",
                    organization: "FlowChat"),
                UserProfile.Create(
                    Id<UserProfile>.New(),
                    "jdoe2",
                    EmailAddress.Create("janet@example.com"),
                    firstName: "Janet",
                    lastName: "Doe",
                    organization: "FlowLab"),
                UserProfile.Create(
                    Id<UserProfile>.New(),
                    "jother",
                    EmailAddress.Create("other@example.com"),
                    firstName: "Jane",
                    lastName: "Other",
                    organization: "FlowChat"));

            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.SearchAsync(null, "Do", "Flow", CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(profile => profile.FriendlyUserId).Should().Equal("jdoe", "jdoe2");
    }

    [Fact]
    public async Task SearchAsync_WhenProfileIsDeleted_DoesNotReturnDeletedProfile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            var activeProfile = UserProfile.Create(
                Id<UserProfile>.New(),
                "jdoe",
                EmailAddress.Create("jane@example.com"),
                firstName: "Jane",
                lastName: "Doe");

            var deletedProfile = UserProfile.Create(
                Id<UserProfile>.New(),
                "jdeleted",
                EmailAddress.Create("deleted@example.com"),
                firstName: "Jane",
                lastName: "Deleted");
            deletedProfile.Delete(UtcDateTimeOffset.Create(new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero)));

            seedContext.UserProfiles.AddRange(activeProfile, deletedProfile);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.SearchAsync("Jane", null, null, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].FriendlyUserId.Should().Be("jdoe");
    }

    [Fact]
    public async Task GetByFriendlyUserIdAsync_WhenFriendlyUserIdHasDifferentCasing_ReturnsProjectedProfile()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                Id<UserProfile>.New(),
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
    public async Task FriendlyUserIdExistsAsync_WhenExcludedUserMatchesFoundProfile_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        Guid userId;
        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                Id<UserProfile>.New(),
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
                Id<UserProfile>.New(),
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

    [Fact]
    public async Task EmailAddressExistsAsync_WhenEmailIsDeleted_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                Id<UserProfile>.New(),
                "jdoe",
                EmailAddress.Create("john@example.com"));

            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();

            var email = profile.Emails.Single();
            seedContext.Entry(email).Property<DateTimeOffset?>("DeletedAt").CurrentValue =
                new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileReadRepository(readContext);

        var result = await repository.EmailAddressExistsAsync(" john@example.com ", CancellationToken.None);

        result.Should().BeFalse();
    }

    private static AppDbContext CreateDbContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
