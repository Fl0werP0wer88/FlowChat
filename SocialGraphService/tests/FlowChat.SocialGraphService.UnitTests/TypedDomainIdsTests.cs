using FlowChat.SocialGraphService.Domain.Common;
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

        var contact = Contact.Create(id, Guid.NewGuid(), Guid.NewGuid(), "user-login");

        Assert.Equal(id, contact.Id);
        Assert.Equal(id.Value, contact.Id.Value);
    }

    [Fact]
    public void Contact_Create_WithGuid_UsesSameGuidInsideTypedId()
    {
        var id = Guid.NewGuid();

        var contact = Contact.Create(id, Guid.NewGuid(), Guid.NewGuid(), "user-login");

        Assert.Equal(id, contact.Id.Value);
    }

    [Fact]
    public void Contact_Create_AssignsProfileData()
    {
        var contact = Contact.Create(
            Id<Contact>.New(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "jkowalski",
            "Jan",
            "Kowalski",
            "+48123456789",
            "jan@example.com");

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
            Id<Contact>.New(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "user-login");

        Assert.Null(contact.FirstName);
        Assert.Null(contact.LastName);
        Assert.Null(contact.PhoneNumber);
        Assert.Null(contact.Email);
    }

    [Fact]
    public void Invitation_Ctor_WithTypedId_AssignsTypedIdValue()
    {
        var id = Id<Invitation>.New();

        var invitation = new Invitation(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            InvitationStatus.Pending);

        Assert.Equal(id, invitation.Id);
        Assert.Equal(id.Value, invitation.Id.Value);
    }

    [Fact]
    public void Invitation_Ctor_WithGuid_UsesSameGuidInsideTypedId()
    {
        var id = Guid.NewGuid();

        var invitation = new Invitation(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            InvitationStatus.Pending);

        Assert.Equal(id, invitation.Id.Value);
    }

    [Fact]
    public void UserSocialGraph_Ctor_WithGuid_UsesSameGuidInsideTypedId()
    {
        var id = Guid.NewGuid();

        var socialGraph = new UserSocialGraph(id, "user-login");

        Assert.Equal(id, socialGraph.Id.Value);
    }

    [Fact]
    public void UserSocialGraph_Ctor_AssignsProfileDataAndVisibility()
    {
        var graphId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var socialGraph = new UserSocialGraph(
            graphId,
            "jkowalski",
            userId,
            "Jan",
            "Kowalski",
            "+48123456789",
            "jan@example.com",
            true,
            false);

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
        var socialGraph = new UserSocialGraph(Guid.NewGuid(), "user-login");

        Assert.Null(socialGraph.FirstName);
        Assert.Null(socialGraph.LastName);
        Assert.Null(socialGraph.PhoneNumber);
        Assert.Null(socialGraph.Email);
    }

    [Fact]
    public void Contact_Create_WithoutLogin_Throws()
    {
        Assert.Throws<ArgumentException>(() => Contact.Create(
            Id<Contact>.New(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ""));
    }

    [Fact]
    public void UserSocialGraph_Ctor_WithoutLogin_Throws()
    {
        Assert.Throws<ArgumentException>(() => new UserSocialGraph(Guid.NewGuid(), ""));
    }

    [Fact]
    public void UserSocialGraph_SendInvitation_AddsInvitation_And_EmitsEvent()
    {
        var socialGraph = new UserSocialGraph(Guid.NewGuid(), "owner-login");
        var invitation = new Invitation(Id<Invitation>.New(), Guid.NewGuid(), Guid.NewGuid());

        socialGraph.SendInvitation(invitation);

        var sentEvent = Assert.Single(socialGraph.DomainEvents);
        Assert.IsType<InvitationSentDomainEvent>(sentEvent);
        Assert.Single(socialGraph.Invitations);
        Assert.Equal(invitation.Id, socialGraph.Invitations[0].Id);
    }

    [Fact]
    public void UserSocialGraph_AcceptInvitation_UpdatesInvitation_AddsContact_And_EmitsEvent()
    {
        var socialGraph = new UserSocialGraph(Guid.NewGuid(), "owner-login");
        var invitation = new Invitation(Id<Invitation>.New(), Guid.NewGuid(), Guid.NewGuid());
        socialGraph.SendInvitation(invitation);
        socialGraph.PopDomainEvents();

        var contact = socialGraph.AcceptInvitation(invitation.Id, "contact-login");

        var acceptedEvent = Assert.Single(socialGraph.DomainEvents);
        Assert.IsType<InvitationAcceptedDomainEvent>(acceptedEvent);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.NotNull(invitation.RespondedAtUtc);
        Assert.Single(socialGraph.Contacts);
        Assert.Equal(contact.Id, socialGraph.Contacts[0].Id);
    }
}
