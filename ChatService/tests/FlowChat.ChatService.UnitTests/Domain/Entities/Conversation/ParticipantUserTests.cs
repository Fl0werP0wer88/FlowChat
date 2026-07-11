using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FluentAssertions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ConversationTests;

public sealed class ParticipantUserTests
{
    [Fact]
    public void Create_WhenLastReadMessageSequenceNumIsNotProvided_DefaultsToZero()
    {
        var participant = ParticipantUser.Create(
            Id<ParticipantUser>.New(),
            Id<ConversationAggregate>.New(),
            Id<UserProfileMarker>.New());

        participant.LastReadMessageSequenceNum.Should().Be(0);
    }

    [Fact]
    public void Create_WhenLastReadMessageSequenceNumIsProvided_StoresValue()
    {
        var participant = ParticipantUser.Create(
            Id<ParticipantUser>.New(),
            Id<ConversationAggregate>.New(),
            Id<UserProfileMarker>.New(),
            lastReadMessageSequenceNum: 42);

        participant.LastReadMessageSequenceNum.Should().Be(42);
    }

    [Fact]
    public void CreateGroupConversation_WhenParticipantsAreCreated_DefaultsLastReadMessageSequenceNumToZero()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var memberId = Id<UserProfileMarker>.New();

        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, memberId],
            "Dev Team");

        conversation.Participants.Should().OnlyContain(p => p.LastReadMessageSequenceNum == 0);
    }

    [Fact]
    public void CreateDuetConversation_WhenParticipantsAreCreated_DefaultsLastReadMessageSequenceNumToZero()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();

        var conversation = DuetConversation.Create(requestingUserId, partnerUserId);

        conversation.Participants.Should().OnlyContain(p => p.LastReadMessageSequenceNum == 0);
    }

    [Fact]
    public void AddParticipants_WhenLastReadMessageSequenceNumIsProvided_StoresValue()
    {
        var creatorId = Id<UserProfileMarker>.New();
        var existingMemberId = Id<UserProfileMarker>.New();
        var newMemberId = Id<UserProfileMarker>.New();
        var conversation = GroupConversation.Create(
            Id<ConversationAggregate>.New(),
            creatorId,
            [creatorId, existingMemberId],
            "Dev Team");

        conversation.AddParticipants([newMemberId], lastReadMessageSequenceNum: 84);

        conversation.Participants.Should().ContainSingle(p =>
            p.UserId == newMemberId &&
            p.LastReadMessageSequenceNum == 84);
    }

}
