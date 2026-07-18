using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FluentAssertions;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ConversationTests;

public sealed class ConversationMembershipTests
{
    [Fact]
    public void Create_WhenCreated_UsesConversationIdAsAggregateId()
    {
        var conversationId = Id<ConversationAggregate>.New();

        var membership = ConversationMembership.Create(
            conversationId,
            ConversationType.Group,
            participantCount: 2);

        membership.Id.Value.Should().Be(conversationId.Value);
        membership.ConversationId.Should().Be(conversationId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CreateGroup_WhenParticipantCountIsBelowMinimum_Throws(int participantCount)
    {
        var act = () => ConversationMembership.Create(
            Id<ConversationAggregate>.New(),
            ConversationType.Group,
            participantCount);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void CreateDuet_WhenParticipantCountIsNotTwo_Throws(int participantCount)
    {
        var act = () => ConversationMembership.Create(
            Id<ConversationAggregate>.New(),
            ConversationType.Duet,
            participantCount);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("One-on-one conversations must have exactly two participants.");
    }

    [Fact]
    public void AddParticipants_WhenGroupMembershipIsValid_IncreasesCount()
    {
        var membership = CreateGroupMembership(participantCount: 2);

        membership.AddParticipants(3);

        membership.ParticipantCount.Should().Be(5);
    }

    [Fact]
    public void RemoveParticipants_WhenGroupWouldDropBelowMinimum_ThrowsWithoutChangingCount()
    {
        var membership = CreateGroupMembership(participantCount: 3);

        var act = () => membership.RemoveParticipants(2);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
        membership.ParticipantCount.Should().Be(3);
    }

    [Fact]
    public void AddParticipants_WhenConversationIsDuet_ThrowsWithoutChangingCount()
    {
        var membership = ConversationMembership.Create(
            Id<ConversationAggregate>.New(),
            ConversationType.Duet,
            participantCount: 2);

        var act = () => membership.AddParticipants(1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("One-on-one conversations must have exactly two participants.");
        membership.ParticipantCount.Should().Be(2);
    }

    private static ConversationMembership CreateGroupMembership(int participantCount)
    {
        return ConversationMembership.Create(
            Id<ConversationAggregate>.New(),
            ConversationType.Group,
            participantCount);
    }
}
