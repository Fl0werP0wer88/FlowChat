using FlowChat.SocialGraphService.Domain.Common;
using FlowChat.SocialGraphService.Domain.Entities;
using FlowChat.SocialGraphService.Domain.Enums;

namespace FlowChat.SocialGraphService.UnitTests;

public class TypedDomainIdsTests
{
    [Fact]
    public void Contact_Create_WithTypedId_AssignsTypedIdValue()
    {
        var id = Id<Contact>.New();

        var contact = Contact.Create(id, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(id, contact.Id);
        Assert.Equal(id.Value, contact.Id.Value);
    }

    [Fact]
    public void Contact_Create_WithGuid_UsesSameGuidInsideTypedId()
    {
        var id = Guid.NewGuid();

        var contact = Contact.Create(id, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(id, contact.Id.Value);
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

        var socialGraph = new UserSocialGraph(id);

        Assert.Equal(id, socialGraph.Id.Value);
    }
}
