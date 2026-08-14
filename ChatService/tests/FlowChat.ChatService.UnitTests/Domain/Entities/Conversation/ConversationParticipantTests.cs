using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.ConversationV2;
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
        participant.ConversationType.Should().Be(ConversationType.Group);
        participant.DuetPartnerUserId.Should().BeNull();
        participant.LastReadMessageSequenceNum.Should().Be(0);
    }

    [Fact]
    public void Restore_WhenRestored_PreservesConversationType()
    {
        var partnerUserId = Id<UserProfileMarker>.New();
        var participant = ConversationParticipant.Restore(
            Id<ConversationParticipant>.New(),
            Id<ConversationAggregate>.New(),
            ConversationType.Duet,
            Id<UserProfileMarker>.New(),
            partnerUserId,
            displayName: null,
            isBlocked: false,
            isMuted: false,
            isHidden: false,
            joinedAtUtc: UtcDateTimeOffset.UtcNow,
            lastReadMessageSequenceNum: 0);

        participant.ConversationType.Should().Be(ConversationType.Duet);
        participant.DuetPartnerUserId.Should().Be(partnerUserId);
    }

    [Fact]
    public void Create_WhenConversationTypeIsInvalid_ThrowsArgumentException()
    {
        var act = () => ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            Id<ConversationAggregate>.New(),
            (ConversationType)999,
            Id<UserProfileMarker>.New(),
            duetPartnerUserId: null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("conversationType");
    }

    [Fact]
    public void Create_WhenDuetPartnerIsMissing_ThrowsArgumentNullException()
    {
        var act = () => ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            Id<ConversationAggregate>.New(),
            ConversationType.Duet,
            Id<UserProfileMarker>.New(),
            duetPartnerUserId: null);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("duetPartnerUserId");
    }

    [Fact]
    public void Create_WhenDuetPartnerMatchesParticipant_ThrowsArgumentException()
    {
        var userId = Id<UserProfileMarker>.New();

        var act = () => ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            Id<ConversationAggregate>.New(),
            ConversationType.Duet,
            userId,
            userId);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("duetPartnerUserId");
    }

    [Fact]
    public void Create_WhenGroupHasDuetPartner_ThrowsArgumentException()
    {
        var act = () => ConversationParticipant.Create(
            Id<ConversationParticipant>.New(),
            Id<ConversationAggregate>.New(),
            ConversationType.Group,
            Id<UserProfileMarker>.New(),
            Id<UserProfileMarker>.New());

        act.Should().Throw<ArgumentException>()
            .WithParameterName("duetPartnerUserId");
    }

    [Fact]
    public void AdvanceReadCursor_WhenSequenceIncreases_UpdatesReadState()
    {
        var participant = CreateParticipant();

        var wasUpdated = participant.AdvanceReadCursor(42);

        wasUpdated.Should().BeTrue();
        participant.LastReadMessageSequenceNum.Should().Be(42);
    }

    [Theory]
    [InlineData(41)]
    [InlineData(42)]
    public void AdvanceReadCursor_WhenSequenceDoesNotIncrease_DoesNotChangeReadState(long sequenceNum)
    {
        var participant = CreateParticipant();
        participant.AdvanceReadCursor(42);

        var wasUpdated = participant.AdvanceReadCursor(sequenceNum);

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
            ConversationType.Group,
            Id<UserProfileMarker>.New(),
            duetPartnerUserId: null);
    }
}
