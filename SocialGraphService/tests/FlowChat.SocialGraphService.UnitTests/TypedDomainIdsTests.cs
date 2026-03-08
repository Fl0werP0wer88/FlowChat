using FlowChat.Domain.Abstractions;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;
using FlowChat.SocialGraphService.Domain.Events;

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
    public void Invitation_Ctor_WithTypedId_AssignsTypedIdValue()
    {
        var id = Id<Invitation>.New();

        var invitation = Invitation.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            InvitationStatus.Pending,
            id: id);

        Assert.Equal(id, invitation.Id);
        Assert.Equal(id.Value, invitation.Id.Value);
    }

    [Fact]
    public void Invitation_Rehydrate_DoesNotEmitDomainEvents()
    {
        var invitation = Invitation.Rehydrate(
            Guid.NewGuid(),
            Guid.NewGuid(),
            id: Id<Invitation>.New());

        Assert.Empty(invitation.DomainEvents);
    }

    [Fact]
    public void Invitation_Create_WithoutId_GeneratesTypedId()
    {
        var invitation = Invitation.Create(
            requesterId: Guid.NewGuid(),
            addresseeId: Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, invitation.Id.Value);
    }

    [Fact]
    public void Invitation_Create_EmitsInvitationSentDomainEvent()
    {
        var invitation = Invitation.Create(
            requesterId: Guid.NewGuid(),
            addresseeId: Guid.NewGuid(),
            id: Id<Invitation>.New());

        var sentEvent = Assert.Single(invitation.DomainEvents);
        Assert.IsType<InvitationSentDomainEvent>(sentEvent);
    }


    [Fact]
    public void UserSocialGraph_Ctor_WithGuid_UsesSameGuidInsideTypedId()
    {
        var id = Guid.NewGuid();

        var socialGraph = UserSocialGraph.Create("user-login", id: id);

        Assert.Equal(id, socialGraph.Id.Value);
    }

    [Fact]
    public void UserSocialGraph_Create_WithoutId_GeneratesTypedId()
    {
        var socialGraph = UserSocialGraph.Create(login: "user-login");

        Assert.NotEqual(Guid.Empty, socialGraph.Id.Value);
    }

    [Fact]
    public void UserSocialGraph_Ctor_AssignsProfileDataAndVisibility()
    {
        var graphId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var socialGraph = UserSocialGraph.Create(
            "jkowalski",
            userId,
            "Jan",
            "Kowalski",
            "+48123456789",
            "jan@example.com",
            true,
            false,
            id: graphId);

        Assert.Equal(graphId, socialGraph.Id.Value);
        Assert.Equal(userId, socialGraph.UserId);
        Assert.Equal("Jan", socialGraph.FirstName);
        Assert.Equal("Kowalski", socialGraph.LastName);
        Assert.Equal("jkowalski", socialGraph.Login);
        Assert.Equal("+48123456789", socialGraph.PhoneNumber);
        Assert.Equal("jan@example.com", socialGraph.Email);
        Assert.True(socialGraph.IsPhoneVisible);
        Assert.False(socialGraph.IsEmailVisible);
    }

    [Fact]
    public void UserSocialGraph_Ctor_AllowsMissingOptionalProfileData()
    {
        var socialGraph = UserSocialGraph.Create("user-login", id: Guid.NewGuid());

        Assert.Null(socialGraph.FirstName);
        Assert.Null(socialGraph.LastName);
        Assert.Null(socialGraph.PhoneNumber);
        Assert.Null(socialGraph.Email);
    }

    [Fact]
    public void UserSocialGraph_Create_RehydratesContacts()
    {
        var contact = Contact.Create(Guid.NewGuid(), Guid.NewGuid(), "contact-login", id: Id<Contact>.New());

        var socialGraph = UserSocialGraph.Create(
            login: "user-login",
            contacts: [contact],
            id: Guid.NewGuid());

        Assert.Single(socialGraph.Contacts);
        Assert.Equal(contact.Id, socialGraph.Contacts[0].Id);
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

    [Fact]
    public void UserSocialGraph_Ctor_WithoutLogin_Throws()
    {
        Assert.Throws<ArgumentException>(() => UserSocialGraph.Create("", id: Guid.NewGuid()));
    }

    [Fact]
    public void Invitation_Accept_UpdatesStatus_And_EmitsEvent()
    {
        var invitation = Invitation.Create(Guid.NewGuid(), Guid.NewGuid(), id: Id<Invitation>.New());
        invitation.PopDomainEvents();

        invitation.Accept();

        var acceptedEvent = Assert.Single(invitation.DomainEvents);
        Assert.IsType<InvitationAcceptedDomainEvent>(acceptedEvent);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.NotNull(invitation.RespondedAtUtc);
    }

    [Fact]
    public void UserSocialGraph_CreateContactFromInvitation_AddsContact()
    {
        var socialGraph = UserSocialGraph.Create("owner-login", id: Guid.NewGuid());
        var invitation = Invitation.Create(socialGraph.UserId, Guid.NewGuid(), id: Id<Invitation>.New());
        invitation.PopDomainEvents();
        invitation.Accept();
        invitation.PopDomainEvents();

        var contact = socialGraph.CreateContactFromInvitation(invitation, "contact-login");

        Assert.Single(socialGraph.Contacts);
        Assert.Equal(contact.Id, socialGraph.Contacts[0].Id);
    }
}
