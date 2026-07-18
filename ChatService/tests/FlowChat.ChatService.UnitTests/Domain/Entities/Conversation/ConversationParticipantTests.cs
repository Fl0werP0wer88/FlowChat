using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FluentAssertions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ConversationTests;

public sealed class ConversationParticipantTests
{
    [Fact]
    public void Create_WhenCreated_InitializesIndependentParticipantState()
    {
        var participant = CreateParticipant();

        participant.IsBlocked.Should().BeFalse();
        participant.IsMuted.Should().BeFalse();
        participant.IsHidden.Should().BeFalse();
        participant.LastReadMessageSequenceNum.Should().Be(0);
    }

    [Fact]
    public void MarkAsRead_WhenSequenceIncreases_UpdatesReadState()
    {
        var participant = CreateParticipant();

        var wasUpdated = participant.MarkAsRead(42);

        wasUpdated.Should().BeTrue();
        participant.LastReadMessageSequenceNum.Should().Be(42);
    }

    [Theory]
    [InlineData(41)]
    [InlineData(42)]
    public void MarkAsRead_WhenSequenceDoesNotIncrease_DoesNotChangeReadState(long sequenceNum)
    {
        var participant = CreateParticipant();
        participant.MarkAsRead(42);

        var wasUpdated = participant.MarkAsRead(sequenceNum);

        wasUpdated.Should().BeFalse();
        participant.LastReadMessageSequenceNum.Should().Be(42);
    }

    [Fact]
    public void ParticipantPreferences_WhenChanged_AreIdempotent()
    {
        var participant = CreateParticipant();

        participant.Mute().Should().BeTrue();
        participant.Mute().Should().BeFalse();
        participant.Hide().Should().BeTrue();
        participant.Hide().Should().BeFalse();

        participant.IsMuted.Should().BeTrue();
        participant.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void ChangeDisplayName_WhenProvided_NormalizesValue()
    {
        var participant = CreateParticipant();

        var wasUpdated = participant.ChangeDisplayName("  Conversation nickname  ");

        wasUpdated.Should().BeTrue();
        participant.DisplayName.Should().Be("Conversation nickname");
    }

    private static ConversationParticipant CreateParticipant()
    {
        return ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            Id<ConversationAggregate>.New(),
            Id<UserProfileMarker>.New());
    }
}
