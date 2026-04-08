using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FluentAssertions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class TypedDomainIdsTests
{
    [Fact]
    public void Contact_Create_WithTypedId_AssignsTypedIdValue()
    {
        var id = Id<Contact>.New();

        var contact = Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "user-login", id: id);

        contact.Id.Should().Be(id);
        contact.Id.Value.Should().Be(id.Value);
    }

    [Fact]
    public void Contact_Create_WithGuid_UsesSameGuidInsideTypedId()
    {
        var id = Guid.NewGuid();

        var contact = Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "user-login", id: id);

        contact.Id.Value.Should().Be(id);
    }

    [Fact]
    public void Contact_Create_AssignsProfileData()
    {
        var contact = Contact.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "jkowalski",
            "Jan",
            "Kowalski",
            PhoneNumber.Create("+48123456789"),
            EmailAddress.Create("jan@example.com"),
            id: Id<Contact>.New());

        contact.FirstName.Should().Be("Jan");
        contact.LastName.Should().Be("Kowalski");
        contact.DisplayName.Should().Be("jkowalski");
        contact.PhoneNumber!.Value.Should().Be("+48123456789");
        contact.EmailAddress!.Value.Should().Be("jan@example.com");
    }

    [Fact]
    public void Contact_Create_AllowsMissingOptionalProfileData()
    {
        var contact = Contact.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-login",
            id: Id<Contact>.New());

        contact.FirstName.Should().BeNull();
        contact.LastName.Should().BeNull();
        contact.PhoneNumber.Should().BeNull();
        contact.EmailAddress.Should().BeNull();
    }

    [Fact]
    public void Contact_Create_WithoutId_GeneratesTypedId()
    {
        var contact = Contact.Create(
            ownerUserId: Guid.NewGuid(),
            contactUserId: Guid.NewGuid(),
            displayName: "user-login");

        contact.Id.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Contact_Rehydrate_DoesNotEmitDomainEvents()
    {
        var contact = Contact.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-login",
            id: Id<Contact>.New());

        contact.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Contact_Create_WithoutDisplayName_Throws()
    {
        var act = () => Contact.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            string.Empty,
            id: Id<Contact>.New());

        act.Should().Throw<ArgumentException>();
    }
}
