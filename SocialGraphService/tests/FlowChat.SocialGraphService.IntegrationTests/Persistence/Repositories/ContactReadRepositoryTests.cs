using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance.Auditing;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Persistence;
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
        Guid newerContactId;
        Guid olderContactId;

        await using (var seedContext = CreateDbContext(databaseName))
        {
            var olderContact = Contact.Create(
                ownerUserId,
                Guid.NewGuid(),
                "Older Contact",
                "Older",
                "Person",
                PhoneNumber.Create("+48111111111"),
                EmailAddress.Create("older@example.com"));

            await Task.Delay(20);

            var newerContact = Contact.Create(
                ownerUserId,
                Guid.NewGuid(),
                "Newer Contact",
                isBlocked: true);

            var ignoredContact = Contact.Create(
                anotherOwnerUserId,
                Guid.NewGuid(),
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

        result[0].DisplayedName.Should().Be("Newer Contact");
        result[0].IsBlocked.Should().BeTrue();
        result[0].PhoneNumber.Should().BeNull();
        result[0].Email.Should().BeNull();

        result[1].DisplayedName.Should().Be("Older Contact");
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
