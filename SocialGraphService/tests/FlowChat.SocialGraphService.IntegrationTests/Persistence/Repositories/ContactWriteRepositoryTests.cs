using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class ContactWriteRepositoryTests
{
    [Fact]
    public async Task ExistsAsync_WhenMatchingContactExists_ReturnsTrue()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var ownerUserId = Id<UserProfileMarker>.New();
        var contactUserId = Id<UserProfileMarker>.New();

        await using (var seedContext = CreateDbContext(connection))
        {
            seedContext.Contacts.Add(Contact.Create(Id<Contact>.New(), ownerUserId, contactUserId, "Jane Doe"));
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(connection);
        var repository = new ContactWriteRepository(context);

        var result = await repository.ExistsAsync(ownerUserId, contactUserId, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenMatchingContactDoesNotExist_ReturnsFalse()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = CreateDbContext(connection);
        var repository = new ContactWriteRepository(context);

        var result = await repository.ExistsAsync(Id<UserProfileMarker>.New(), Id<UserProfileMarker>.New(), CancellationToken.None);

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
