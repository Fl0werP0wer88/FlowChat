using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.IntegrationTests.Persistence.Repositories;

public sealed class ContactReadRepositoryTests
{
    [Fact]
    public async Task GetForUserAsync_WhenContactsExist_ReturnsOnlyMatchingContactsOrderedByCreatedAtDescending()
    {
        var databaseName = Guid.NewGuid().ToString();
        var ownerUserId = Guid.NewGuid();
        var ownerProfileId = Id<UserProfileMarker>.FromGuid(ownerUserId);
        var anotherOwnerUserId = Id<UserProfileMarker>.New();
        Guid newerContactId;
        Guid olderContactId;

        await using (var seedContext = CreateDbContext(databaseName))
        {
            var olderContact = Contact.Create(
                Id<Contact>.New(),
                ownerProfileId,
                Id<UserProfileMarker>.New(),
                "Older Contact",
                "Older",
                "Person",
                PhoneNumber.Create("+48111111111"),
                EmailAddress.Create("older@example.com"));

            await Task.Delay(20);

            var newerContact = Contact.Create(
                Id<Contact>.New(),
                ownerProfileId,
                Id<UserProfileMarker>.New(),
                "Newer Contact",
                isBlocked: true);

            var ignoredContact = Contact.Create(
                Id<Contact>.New(),
                anotherOwnerUserId,
                Id<UserProfileMarker>.New(),
                "Ignored Contact");

            olderContactId = olderContact.Id.Value;
            newerContactId = newerContact.Id.Value;

            seedContext.Contacts.AddRange(olderContact, newerContact, ignoredContact);
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
