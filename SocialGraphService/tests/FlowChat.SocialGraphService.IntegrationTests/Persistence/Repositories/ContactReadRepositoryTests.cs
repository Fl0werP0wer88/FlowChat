using FlowChat.Shared.Persistance.Auditing;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Entities;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class ContactReadRepositoryTests
{
    [Fact]
    public async Task GetForUserAsync_WhenContactsExist_ReturnsOnlyMatchingContactsOrderedByCreatedAtDescending()
    {
        var databaseName = Guid.NewGuid().ToString();
        var ownerUserId = Guid.NewGuid();
        var anotherOwnerUserId = Guid.NewGuid();
        var olderContactId = Guid.NewGuid();
        var newerContactId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(databaseName))
        {
            seedContext.ContactReads.AddRange(
                new ContactReadEntity
                {
                    Id = olderContactId,
                    OwnerUserId = ownerUserId,
                    ContactUserId = Guid.NewGuid(),
                    DisplayName = "Older Contact",
                    FirstName = "Older",
                    LastName = "Person",
                    PhoneNumber = "+48111111111",
                    EmailAddress = "older@example.com",
                    CreatedAtUtc = new DateTimeOffset(2026, 4, 24, 8, 0, 0, TimeSpan.Zero)
                },
                new ContactReadEntity
                {
                    Id = newerContactId,
                    OwnerUserId = ownerUserId,
                    ContactUserId = Guid.NewGuid(),
                    DisplayName = "Newer Contact",
                    IsBlocked = true,
                    CreatedAtUtc = new DateTimeOffset(2026, 4, 24, 9, 0, 0, TimeSpan.Zero)
                },
                new ContactReadEntity
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = anotherOwnerUserId,
                    ContactUserId = Guid.NewGuid(),
                    DisplayName = "Ignored Contact",
                    CreatedAtUtc = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero)
                });
            await seedContext.SaveChangesAsync();
        }

        await using var readContext = CreateDbContext(databaseName);
        var repository = new ContactReadRepository(readContext);

        var result = await repository.GetForUserAsync(ownerUserId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(contact => contact.Id).Should().Equal(newerContactId, olderContactId);

        result[0].DisplayName.Should().Be("Newer Contact");
        result[0].IsBlocked.Should().BeTrue();
        result[0].PhoneNumber.Should().BeNull();
        result[0].Email.Should().BeNull();

        result[1].DisplayName.Should().Be("Older Contact");
        result[1].FirstName.Should().Be("Older");
        result[1].LastName.Should().Be("Person");
        result[1].PhoneNumber.Should().Be("+48111111111");
        result[1].Email.Should().Be("older@example.com");
        result.All(contact => contact.OwnerUserId == ownerUserId).Should().BeTrue();
    }

    private static AppDbContext CreateDbContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new EntityBaseSaveChangesInterceptor())
            .Options;

        var context = new AppDbContext(options);
        return context;
    }
}
