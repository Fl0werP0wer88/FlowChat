using FlowChat.ChatService.Domain.Entities.Conversation;
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
    public void RemoveParticipants_WhenParticipantsExist_RemovesThem()
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
