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
    public void Create_WhenCreated_EmitsOneAddedEventWithParticipantsInInputOrderAndZeroReadCursor()
    {
        var participantUserIds = CreateUserIds(2);

        var membership = ConversationMembership.Create(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            participantUserIds);

        var addedEvent = membership.DomainEvents
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ConversationParticipantsAddedDomainEventV2>()
            .Which;
        addedEvent.ConversationType.Should().Be(ConversationType.Group);
        addedEvent.ParticipantUserIds.Should().Equal(participantUserIds);
        addedEvent.InitialReadCursor.Should().Be(0);
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
    public void AddParticipants_WhenGroupMembershipIsValid_EmitsOneEventWithParticipantsInInputOrder()
    {
        var membership = CreateGroupMembership(participantCount: 2);
        var addedUserIds = CreateUserIds(3);
        membership.ClearEvents();

        membership.AddParticipants(addedUserIds, initialReadCursor: 42);

        membership.ParticipantCount.Should().Be(5);
        var addedEvent = membership.DomainEvents
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ConversationParticipantsAddedDomainEventV2>()
            .Which;
        addedEvent.ConversationType.Should().Be(ConversationType.Group);
        addedEvent.ParticipantUserIds.Should().Equal(addedUserIds);
        addedEvent.InitialReadCursor.Should().Be(42);
    }

    [Fact]
    public void AddParticipants_WhenInitialReadCursorIsNegative_ThrowsWithoutChangingStateOrEvents()
    {
        var membership = CreateGroupMembership(participantCount: 2);
        membership.ClearEvents();

        var act = () => membership.AddParticipants(
            CreateUserIds(1),
            initialReadCursor: -1);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("Initial read cursor cannot be negative.*");
        membership.ParticipantCount.Should().Be(2);
        membership.DomainEvents.Should().BeEmpty();
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
    public void RemoveParticipants_WhenValid_EmitsOneEventWithParticipantsInInputOrder()
    {
        var membership = CreateGroupMembership(participantCount: 4);
        var removedUserIds = CreateUserIds(2);
        membership.ClearEvents();

        membership.RemoveParticipants(removedUserIds);

        membership.ParticipantCount.Should().Be(2);
        var removedEvent = membership.DomainEvents
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<ConversationParticipantsRemovedDomainEventV2>()
            .Which;
        removedEvent.ParticipantUserIds.Should().Equal(removedUserIds);
    }

    [Fact]
    public void AddParticipants_WhenInputContainsDuplicate_ThrowsWithoutChangingStateOrEvents()
    {
        var membership = CreateGroupMembership(participantCount: 2);
        var duplicatedUserId = Id<UserProfileMarker>.New();
        membership.ClearEvents();

        var act = () => membership.AddParticipants(
            [duplicatedUserId, duplicatedUserId],
            initialReadCursor: 0);

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

        var act = () => membership.AddParticipants(
            CreateUserIds(1),
            initialReadCursor: 0);

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
