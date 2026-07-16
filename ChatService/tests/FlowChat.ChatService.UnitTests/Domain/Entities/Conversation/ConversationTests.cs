using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using FluentAssertions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ConversationTests;

public sealed class ConversationTests
{
    [Fact]
    public void CreateGroupConversation_WhenCreated_DefaultsLastMsgSequenceNumToZero()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId],
            "Dev Team");

        conversation.LastMsgSequenceNum.Should().Be(0);
        conversation.MembershipRevision.Should().Be(1);
    }

    [Fact]
    public void CreateGroupConversation_WhenAccessedAsTypedEntity_UsesConversationId()
    {
        var conversation = CreateGroupConversation();

        var typedId = ((IEntity<GroupConversation>)conversation).Id;

        typedId.Value.Should().Be(conversation.Id.Value);
    }

    [Fact]
    public void CreateGroupConversation_WhenCreatorNotInParticipantUserIds_StillIncludesCreatorAsParticipant()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [memberId],
            "Dev Team");

        conversation.Participants.Should().Contain(p => p.UserId == creatorId);
        conversation.Participants.Should().HaveCount(2);
    }

    [Fact]
    public void CreateGroupConversation_WhenLessThanTwoParticipants_ThrowsInvalidOperationException()
    {
        var creatorId = Id<UserProfileMarker>.New();

        var act = () => GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId],
            "Dev Team");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
    }

    [Fact]
    public void CreateDuetConversation_WhenCreated_DefaultsLastMsgSequenceNumToZero()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();

        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);

        conversation.LastMsgSequenceNum.Should().Be(0);
    }

    [Fact]
    public void SetSequenceNumber_WhenGreaterThanCurrent_SetsLastMsgSequenceNum()
    {
        var conversation = CreateGroupConversation();

        conversation.SetSequenceNumber(42);

        conversation.LastMsgSequenceNum.Should().Be(42);
    }

    [Fact]
    public void SetSequenceNumber_WhenEqualToCurrent_LeavesLastMsgSequenceNumUnchanged()
    {
        var conversation = CreateGroupConversation();
        conversation.SetSequenceNumber(42);

        conversation.SetSequenceNumber(42);

        conversation.LastMsgSequenceNum.Should().Be(42);
    }

    [Fact]
    public void SetSequenceNumber_WhenLowerThanCurrent_ThrowsArgumentException()
    {
        var conversation = CreateGroupConversation();
        conversation.SetSequenceNumber(42);

        var act = () => conversation.SetSequenceNumber(41);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("sequenceNum");
        conversation.LastMsgSequenceNum.Should().Be(42);
    }

    [Fact]
    public void CreateGroupConversation_WhenCreated_RaisesCreatedDomainEvent()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId],
            "Dev Team");

        conversation.DomainEvents.OfType<GroupConversationCreatedDomainEvent>().Should().ContainSingle();
    }

    [Fact]
    public void AddParticipants_WhenParticipantsAdded_IncrementsMembershipRevision()
    {
        var conversation = CreateGroupConversation();
        var newMemberId = Id<UserProfileMarker>.New();

        conversation.AddParticipants([newMemberId]);

        conversation.Participants.Should().Contain(participant => participant.UserId == newMemberId);
        conversation.MembershipRevision.Should().Be(2);
    }

    [Fact]
    public void RemoveParticipants_WhenParticipantsExist_IncrementsMembershipRevision()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();
        var removedMemberId = Id<UserProfileMarker>.New();
        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId, removedMemberId],
            "Dev Team");

        conversation.RemoveParticipants([removedMemberId]);

        conversation.Participants.Should().NotContain(participant => participant.UserId == removedMemberId);
        conversation.MembershipRevision.Should().Be(2);
    }

    [Fact]
    public void AddParticipants_WhenBatchIsEmpty_ThrowsWithoutIncrementingMembershipRevision()
    {
        var conversation = CreateGroupConversation();

        var act = () => conversation.AddParticipants([]);

        act.Should().Throw<ArgumentException>();
        conversation.MembershipRevision.Should().Be(1);
    }

    [Fact]
    public void RemoveParticipants_WhenBatchIsEmpty_ThrowsWithoutIncrementingMembershipRevision()
    {
        var conversation = CreateGroupConversation();

        var act = () => conversation.RemoveParticipants([]);

        act.Should().Throw<ArgumentException>();
        conversation.MembershipRevision.Should().Be(1);
    }

    [Fact]
    public void NonMembershipChanges_DoNotIncrementMembershipRevision()
    {
        var conversation = CreateGroupConversation();
        var participantId = conversation.Participants.First().UserId;

        conversation.SetSequenceNumber(1);
        conversation.MuteParticipant(participantId);
        conversation.HideParticipant(participantId);
        conversation.BlockParticipant(participantId);

        conversation.MembershipRevision.Should().Be(1);
    }

    [Fact]
    public void RemoveParticipants_WhenUserIsNotParticipant_DoesNotIncrementMembershipRevision()
    {
        var conversation = CreateGroupConversation();
        var nonParticipantId = Id<UserProfileMarker>.New();

        var act = () => conversation.RemoveParticipants([nonParticipantId]);

        act.Should().Throw<InvalidOperationException>();
        conversation.MembershipRevision.Should().Be(1);
    }

    [Fact]
    public void RemoveParticipants_WhenParticipants_RemovesThem()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();
        var removedMemberId = Id<UserProfileMarker>.New();
        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId, removedMemberId],
            "Dev Team");

        conversation.RemoveParticipants([removedMemberId]);

        conversation.Participants.Should().NotContain(p => p.UserId == removedMemberId);
        conversation.Participants.Should().HaveCount(2);
    }

    [Fact]
    public void RemoveParticipants_WhenUserIsNotParticipant_ThrowsInvalidOperationException()
    {
        var conversation = CreateGroupConversation();
        var nonParticipantId = Id<UserProfileMarker>.New();

        var act = () => conversation.RemoveParticipants([nonParticipantId]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User is not a participant in this conversation.");
    }

    [Fact]
    public void RemoveParticipants_WhenGroupWouldHaveFewerThanTwoParticipants_ThrowsInvalidOperationException()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();
        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId],
            "Dev Team");

        var act = () => conversation.RemoveParticipants([memberId]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
        conversation.Participants.Should().HaveCount(2);
    }

    [Fact]
    public void AddParticipants_WhenGroupWouldHaveFewerThanTwoParticipants_ThrowsInvalidOperationException()
    {
        var conversation = CreateGroupConversationWithoutParticipants();
        var memberId = Id<UserProfileMarker>.New();

        var act = () => conversation.AddParticipants([memberId]);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
        conversation.Participants.Should().BeEmpty();
    }

    [Fact]
    public void GetParticipant_WhenParticipantExists_ReturnsParticipant()
    {
        var conversation = CreateGroupConversation();
        var existingUserId = conversation.Participants.First().UserId;

        var participant = conversation.GetParticipant(existingUserId);

        participant.Should().NotBeNull();
        participant!.UserId.Should().Be(existingUserId);
    }

    [Fact]
    public void GetParticipant_WhenParticipantDoesNotExist_ReturnsNull()
    {
        var conversation = CreateGroupConversation();

        var participant = conversation.GetParticipant(Id<UserProfileMarker>.New());

        participant.Should().BeNull();
    }

    [Fact]
    public void BlockParticipant_WhenParticipantExists_SetsIsBlockedTrue()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);

        conversation.BlockParticipant(requestingUserId);

        conversation.GetParticipant(requestingUserId)!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public void UnblockParticipant_WhenParticipantIsBlocked_SetsIsBlockedFalse()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);
        conversation.BlockParticipant(requestingUserId);

        conversation.UnblockParticipant(requestingUserId);

        conversation.GetParticipant(requestingUserId)!.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void MuteParticipant_WhenParticipantExists_SetsIsMutedTrue()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);

        var result = conversation.MuteParticipant(requestingUserId);

        result.Should().BeTrue();
        conversation.GetParticipant(requestingUserId)!.IsMuted.Should().BeTrue();
    }

    [Fact]
    public void UnmuteParticipant_WhenParticipantIsMuted_SetsIsMutedFalse()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);
        conversation.MuteParticipant(requestingUserId);

        var result = conversation.UnmuteParticipant(requestingUserId);

        result.Should().BeTrue();
        conversation.GetParticipant(requestingUserId)!.IsMuted.Should().BeFalse();
    }

    [Fact]
    public void HideParticipant_WhenParticipantExists_SetsIsHiddenTrue()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);

        var result = conversation.HideParticipant(requestingUserId);

        result.Should().BeTrue();
        conversation.GetParticipant(requestingUserId)!.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void UnhideParticipant_WhenParticipantIsHidden_SetsIsHiddenFalse()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();
        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);
        conversation.HideParticipant(requestingUserId);

        var result = conversation.UnhideParticipant(requestingUserId);

        result.Should().BeTrue();
        conversation.GetParticipant(requestingUserId)!.IsHidden.Should().BeFalse();
    }

    [Fact]
    public void BlockParticipant_WhenUserIsNotParticipant_ThrowsInvalidOperationException()
    {
        var conversation = CreateGroupConversation();

        var act = () => conversation.BlockParticipant(Id<UserProfileMarker>.New());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("User is not a participant in this conversation.");
    }

    private static GroupConversation CreateGroupConversation()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();

        return GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId],
            "Dev Team");
    }

    private static GroupConversation CreateGroupConversationWithoutParticipants()
    {
        var constructor = typeof(GroupConversation).GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            binder: null,
            [
                typeof(Id<ConversationAggregate>),
                typeof(ConversationType),
                typeof(string),
                typeof(Id<UserProfileMarker>),
                typeof(long)
            ],
            modifiers: null);

        constructor.Should().NotBeNull();

        return (GroupConversation)constructor!.Invoke([
            Id<ConversationAggregate>.New(),
            ConversationType.Group,
            "Dev Team",
            Id<UserProfileMarker>.New(),
            0L
        ]);
    }
}
