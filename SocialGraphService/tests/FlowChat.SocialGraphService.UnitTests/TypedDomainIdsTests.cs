using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Entities;

namespace FlowChat.SocialGraphService.UnitTests;

public class TypedDomainIdsTests
{
    [Fact]
    public void Contact_Create_WithTypedId_AssignsTypedIdValue()
    {
        var id = Id<Contact>.New();

        var contact = Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "user-login", id: id);

        Assert.Equal(id, contact.Id);
        Assert.Equal(id.Value, contact.Id.Value);
    }

    [Fact]
    public void Contact_Create_WithGuid_UsesSameGuidInsideTypedId()
    {
        var id = Guid.NewGuid();

        var contact = Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "user-login", id: id);

        Assert.Equal(id, contact.Id.Value);
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
            "+48123456789",
            "jan@example.com",
            id: Id<Contact>.New());

        Assert.Equal("Jan", contact.FirstName);
        Assert.Equal("Kowalski", contact.LastName);
        Assert.Equal("jkowalski", contact.Login);
        Assert.Equal("+48123456789", contact.PhoneNumber);
        Assert.Equal("jan@example.com", contact.Email);
    }

    [Fact]
    public void Contact_Create_AllowsMissingOptionalProfileData()
    {
        var contact = Contact.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-login",
            id: Id<Contact>.New());

        Assert.Null(contact.FirstName);
        Assert.Null(contact.LastName);
        Assert.Null(contact.PhoneNumber);
        Assert.Null(contact.Email);
    }

    [Fact]
    public void Contact_Create_WithoutId_GeneratesTypedId()
    {
        var contact = Contact.Create(
            ownerUserId: Guid.NewGuid(),
            contactUserId: Guid.NewGuid(),
            login: "user-login");

        Assert.NotEqual(Guid.Empty, contact.Id.Value);
    }

    [Fact]
    public void Contact_Rehydrate_DoesNotEmitDomainEvents()
    {
        var contact = Contact.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-login",
            id: Id<Contact>.New());

        Assert.Empty(contact.DomainEvents);
    }

    [Fact]
    public void Contact_Create_WithoutLogin_Throws()
    {
        Assert.Throws<ArgumentException>(() => Contact.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "",
            id: Id<Contact>.New()));
    }

}
