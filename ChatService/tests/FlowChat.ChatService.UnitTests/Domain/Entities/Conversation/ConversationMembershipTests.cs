using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using FluentAssertions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ConversationTests;

public sealed class ConversationMembershipTests
{
    [Fact]
    public void Create_WhenCreated_UsesConversationIdAsAggregateId()
    {
        var conversationId = Id<ConversationV2>.New();

        var membership = ConversationMembership.Create(
            conversationId,
            ConversationType.Group,
            CreateUserIds(2));

        membership.Id.Value.Should().Be(conversationId.Value);
        membership.ConversationId.Should().Be(conversationId);
        membership.ParticipantCount.Should().Be(2);
    }

    [Fact]
    public void Create_WhenCreated_EmitsOneEventPerParticipantAndSingleDelta()
    {
        var participantUserIds = CreateUserIds(2);

        var membership = ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            participantUserIds);

        membership.DomainEvents
            .OfType<ConversationParticipantAddedDomainEventV2>()
            .Select(domainEvent => domainEvent.UserId)
            .Should()
            .BeEquivalentTo(participantUserIds);
        var delta = membership.DomainEvents
            .OfType<ConversationMembershipDeltaDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        delta.Operation.Should().Be(ConversationMembershipDeltaOperation.Added);
        delta.UserIds.Should().BeEquivalentTo(participantUserIds);
        delta.ParticipantCount.Should().Be(2);
    }

    [Fact]
    public void CreateGroup_WhenParticipantListIsEmpty_Throws()
    {
        var act = () => ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            []);

        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one participant must be provided.*");
    }

    [Fact]
    public void CreateGroup_WhenParticipantCountIsBelowMinimum_Throws()
    {
        var act = () => ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            CreateUserIds(1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void CreateDuet_WhenParticipantCountIsNotTwo_Throws(int participantCount)
    {
        var act = () => ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Duet,
            CreateUserIds(participantCount));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("One-on-one conversations must have exactly two participants.");
    }

    [Fact]
    public void AddParticipants_WhenGroupMembershipIsValid_IncreasesCountAndEmitsExactDelta()
    {
        var membership = CreateGroupMembership(participantCount: 2);
        var addedUserIds = CreateUserIds(3);
        membership.ClearEvents();

        membership.AddParticipants(addedUserIds);

        membership.ParticipantCount.Should().Be(5);
        membership.DomainEvents
            .OfType<ConversationParticipantAddedDomainEventV2>()
            .Should()
            .HaveCount(3);
        var delta = membership.DomainEvents
            .OfType<ConversationMembershipDeltaDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        delta.Operation.Should().Be(ConversationMembershipDeltaOperation.Added);
        delta.UserIds.Should().Equal(addedUserIds);
        delta.ParticipantCount.Should().Be(5);
    }

    [Fact]
    public void RemoveParticipants_WhenGroupWouldDropBelowMinimum_ThrowsWithoutChangingStateOrEvents()
    {
        var membership = CreateGroupMembership(participantCount: 3);
        membership.ClearEvents();

        var act = () => membership.RemoveParticipants(CreateUserIds(2));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
        membership.ParticipantCount.Should().Be(3);
        membership.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveParticipants_WhenValid_EmitsOneEventPerParticipantAndSingleDelta()
    {
        var membership = CreateGroupMembership(participantCount: 4);
        var removedUserIds = CreateUserIds(2);
        membership.ClearEvents();

        membership.RemoveParticipants(removedUserIds);

        membership.ParticipantCount.Should().Be(2);
        membership.DomainEvents
            .OfType<ConversationParticipantRemovedDomainEventV2>()
            .Should()
            .HaveCount(2);
        var delta = membership.DomainEvents
            .OfType<ConversationMembershipDeltaDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        delta.Operation.Should().Be(ConversationMembershipDeltaOperation.Removed);
        delta.UserIds.Should().Equal(removedUserIds);
        delta.ParticipantCount.Should().Be(2);
    }

    [Fact]
    public void AddParticipants_WhenInputContainsDuplicate_ThrowsWithoutChangingStateOrEvents()
    {
        var membership = CreateGroupMembership(participantCount: 2);
        var duplicatedUserId = Id<UserProfileMarker>.New();
        membership.ClearEvents();

        var act = () => membership.AddParticipants([duplicatedUserId, duplicatedUserId]);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot contain duplicates*");
        membership.ParticipantCount.Should().Be(2);
        membership.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void AddParticipants_WhenConversationIsDuet_ThrowsWithoutChangingStateOrEvents()
    {
        var membership = ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Duet,
            CreateUserIds(2));
        membership.ClearEvents();

        var act = () => membership.AddParticipants(CreateUserIds(1));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("One-on-one conversations must have exactly two participants.");
        membership.ParticipantCount.Should().Be(2);
        membership.DomainEvents.Should().BeEmpty();
    }

    private static ConversationMembership CreateGroupMembership(int participantCount)
    {
        return ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            CreateUserIds(participantCount));
    }

    private static IReadOnlyCollection<Id<UserProfileMarker>> CreateUserIds(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Id<UserProfileMarker>.New())
            .ToArray();
}
