using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
using FlowChat.SocialGraphService.Domain.Entities.Contact.Events;
using FluentAssertions;
using UserProfileMarker = FlowChat.SocialGraphService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class ContactTests
{
    [Fact]
    public void Create_WhenOwnerUserIdIsEmpty_ThrowsArgumentException()
    {
        var act = () => Id<UserProfileMarker>.FromGuid(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenContactUserIdIsEmpty_ThrowsArgumentException()
    {
        var act = () => Id<UserProfileMarker>.FromGuid(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhenOwnerAndContactUsersAreSame_ThrowsArgumentException()
    {
        var userId = Id<UserProfileMarker>.New();

        var act = () => Contact.Create(Id<Contact>.New(), userId, userId, "John Doe");

        act.Should().Throw<ArgumentException>()
            .WithMessage("OwnerUserId and ContactUserId must be different.");
    }

    [Fact]
    public void Create_WhenContactIsCreated_RaisesContactAddedDomainEvent()
    {
        var contact = Contact.Create(Id<Contact>.New(), Id<UserProfileMarker>.New(), Id<UserProfileMarker>.New(), "John Doe");

        contact.DomainEvents.OfType<ContactAddedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void MarkDeleted_WhenCalled_RaisesContactDeletedDomainEvent()
    {
        var contact = Contact.Create(Id<Contact>.New(), Id<UserProfileMarker>.New(), Id<UserProfileMarker>.New(), "John Doe");
        contact.PopDomainEvents();

        contact.MarkDeleted();

        contact.DomainEvents.OfType<ContactDeletedDomainEvent>().Should().ContainSingle();
    }
}
