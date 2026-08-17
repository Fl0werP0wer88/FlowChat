using FlowChat.ChatService.Domain.Entities.Conversation.ValueObjects;
using FlowChat.Shared.Domain;
using FluentAssertions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.Conversation.ValueObjects;

public sealed class DuetParticipantPairTests
{
    [Fact]
    public void Create_WhenUsersAreReversed_NormalizesOrder()
    {
        var lowerUserId = Id<UserProfileMarker>.FromGuid(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var higherUserId = Id<UserProfileMarker>.FromGuid(Guid.Parse("00000000-0000-0000-0000-000000000002"));

        var pair = DuetParticipantPair.Create(higherUserId, lowerUserId);

        pair.FirstUserId.Should().Be(lowerUserId);
        pair.SecondUserId.Should().Be(higherUserId);
    }

    [Fact]
    public void Create_WhenPairsContainSameUsers_AreEqualByValue()
    {
        var firstUserId = Id<UserProfileMarker>.New();
        var secondUserId = Id<UserProfileMarker>.New();

        var firstPair = DuetParticipantPair.Create(firstUserId, secondUserId);
        var secondPair = DuetParticipantPair.Create(secondUserId, firstUserId);

        firstPair.Should().Be(secondPair);
        (firstPair == secondPair).Should().BeTrue();
        firstPair.GetHashCode().Should().Be(secondPair.GetHashCode());
    }

    [Fact]
    public void Create_WhenUsersAreTheSame_Throws()
    {
        var userId = Id<UserProfileMarker>.New();

        var act = () => DuetParticipantPair.Create(userId, userId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("One-on-one conversations require two distinct participants.*");
    }
}
