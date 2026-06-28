using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using FlowChat.UserProfileService.Persistence;
using FlowChat.UserProfileService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.IntegrationTests.Persistence.Repositories;

public sealed class UserProfileWriteRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_WhenProfileExists_LoadsEmailsAndPhones()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var profileId = Id<UserProfile>.New();
        await using (var seedContext = CreateDbContext(connection))
        {
            var profile = UserProfile.Create(
                profileId,
                "jdoe",
                EmailAddress.Create("john@example.com"),
                PhoneNumber.Create("+48123123123"));
            profile.AddEmail(Id<Email>.New(), EmailAddress.Create("john.secondary@example.com"));

            seedContext.UserProfiles.Add(profile);
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(connection);
        var repository = new UserProfileWriteRepository(readContext);

        var result = await repository.GetByIdAsync(profileId.Value, CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(profileId);
        result.Emails.Should().HaveCount(2);
        result.Emails.Should().ContainSingle(email => email.IsMain && email.Address.Value == "john@example.com");
        result.Emails.Should().OnlyContain(email => email.IsVisible);
        result.Phones.Should().ContainSingle(phone => phone.IsMain && phone.Number.Value == "+48123123123");
        result.Phones.Should().OnlyContain(phone => phone.IsVisible);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProfileDoesNotExist_ReturnsNull()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateDbContext(connection);
        var repository = new UserProfileWriteRepository(context);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
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
