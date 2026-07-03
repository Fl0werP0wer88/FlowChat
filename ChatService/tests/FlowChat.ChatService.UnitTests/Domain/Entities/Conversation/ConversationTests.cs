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
    public void Restore_WhenLastMsgSequenceNumProvided_StoresValue()
    {
        var conversationId = Id<ConversationAggregate>.New();
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();
        var participants = new[]
        {
            ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, creatorId),
            ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, memberId)
        };

        var conversation = GroupConversation.Restore(
            conversationId,
            "Dev Team",
            creatorId,
            lastMsgSequenceNum: 84,
            participants);

        conversation.LastMsgSequenceNum.Should().Be(84);
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
}
