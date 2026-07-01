using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Domain.Entities.Contact;
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

}
