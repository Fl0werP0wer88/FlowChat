using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.Shared.Domain;
using FluentAssertions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ConversationTests;

public sealed class ConversationV2Tests
{
    [Fact]
    public void CreateGroup_WhenValid_NormalizesMetadataAndEmitsCompleteInitialComposition()
    {
        var createdByUserId = Id<UserProfileMarker>.New();
        var participantUserId = Id<UserProfileMarker>.New();

        var conversation = ConversationV2.CreateGroup(
            Id<ConversationV2>.New(),
            createdByUserId,
            [participantUserId, participantUserId],
            "  Group name  ");

        conversation.ConversationType.Should().Be(ConversationType.Group);
        conversation.Name.Should().Be("Group name");
        conversation.CreatedByUserId.Should().Be(createdByUserId);
        var createdEvent = conversation.DomainEvents
            .OfType<ConversationCreatedDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        createdEvent.ParticipantUserIds.Should().BeEquivalentTo(
            [createdByUserId, participantUserId]);
        createdEvent.AggregateType.Should().Be("conversation-v2");
    }

    [Fact]
    public void CreateGroup_WhenCreatorWouldBeOnlyParticipant_Throws()
    {
        var createdByUserId = Id<UserProfileMarker>.New();

        var act = () => ConversationV2.CreateGroup(
            Id<ConversationV2>.New(),
            createdByUserId,
            [createdByUserId],
            "Group name");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Group conversations must have at least two participants.");
    }

    [Fact]
    public void CreateGroup_WhenNameIsBlank_Throws()
    {
        var act = () => ConversationV2.CreateGroup(
            Id<ConversationV2>.New(),
            Id<UserProfileMarker>.New(),
            [Id<UserProfileMarker>.New()],
            " ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("Group conversations must have a name.*");
    }

    [Fact]
    public void CreateDuet_WhenUsersAreDistinct_EmitsCreatedEventWithTwoParticipants()
    {
        var createdByUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();

        var conversation = ConversationV2.CreateDuet(
            createdByUserId,
            partnerUserId);

        conversation.ConversationType.Should().Be(ConversationType.Duet);
        conversation.Name.Should().BeNull();
        conversation.DomainEvents
            .OfType<ConversationCreatedDomainEventV2>()
            .Should()
            .ContainSingle()
            .Which.ParticipantUserIds.Should()
            .BeEquivalentTo([createdByUserId, partnerUserId]);
    }

    [Fact]
    public void CreateDuet_WhenUsersAreTheSame_Throws()
    {
        var userId = Id<UserProfileMarker>.New();

        var act = () => ConversationV2.CreateDuet(userId, userId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("One-on-one conversations must have exactly two distinct participants.");
    }

    [Fact]
    public void Restore_WhenCalled_DoesNotEmitDomainEvent()
    {
        var conversation = ConversationV2.Restore(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            "Group name",
            Id<UserProfileMarker>.New());

        conversation.DomainEvents.Should().BeEmpty();
    }
}
