using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.ChatService.Domain.Entities.Conversation.Events;
using FlowChat.ChatService.Domain.Entities.Conversation.ValueObjects;
using FlowChat.Shared.Domain;
using FluentAssertions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ConversationTests;

public sealed class ConversationV2Tests
{
    [Fact]
    public void CreateGroup_WhenValid_NormalizesMetadataAndEmitsCompleteInitialComposition()
    {
        var requestingUserId = Id<UserProfileMarker>.New();
        var participantUserId = Id<UserProfileMarker>.New();

        var conversation = ConversationV2.CreateGroup(
            Id<ConversationV2>.New(),
            requestingUserId,
            [participantUserId, participantUserId],
            "  Group name  ");

        conversation.ConversationType.Should().Be(ConversationType.Group);
        conversation.Name.Should().Be("Group name");
        conversation.DuetParticipants.Should().BeNull();
        var createdEvent = conversation.DomainEvents
            .OfType<ConversationCreatedDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        createdEvent.ParticipantUserIds.Should().BeEquivalentTo(
            [requestingUserId, participantUserId]);
        createdEvent.AggregateType.Should().Be("conversation-v2");
    }

    [Fact]
    public void CreateGroup_WhenCreatorWouldBeOnlyParticipant_Throws()
    {
        var requestingUserId = Id<UserProfileMarker>.New();

        var act = () => ConversationV2.CreateGroup(
            Id<ConversationV2>.New(),
            requestingUserId,
            [requestingUserId],
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
        var requestingUserId = Id<UserProfileMarker>.New();
        var partnerUserId = Id<UserProfileMarker>.New();

        var conversation = ConversationV2.CreateDuet(
            requestingUserId,
            partnerUserId);

        conversation.ConversationType.Should().Be(ConversationType.Duet);
        conversation.Name.Should().BeNull();
        conversation.DuetParticipants.Should().Be(
            DuetParticipantPair.Create(requestingUserId, partnerUserId));
        conversation.DomainEvents
            .OfType<ConversationCreatedDomainEventV2>()
            .Should()
            .ContainSingle()
            .Which.ParticipantUserIds.Should()
            .BeEquivalentTo([requestingUserId, partnerUserId]);
    }

    [Fact]
    public void CreateDuet_WhenUsersAreTheSame_Throws()
    {
        var userId = Id<UserProfileMarker>.New();

        var act = () => ConversationV2.CreateDuet(userId, userId);

        act.Should().Throw<ArgumentException>()
            .WithMessage("One-on-one conversations require two distinct participants.*");
    }

    [Fact]
    public void Restore_WhenCalled_DoesNotEmitDomainEvent()
    {
        var conversation = ConversationV2.Restore(
            Id<ConversationV2>.New(),
            ConversationType.Group,
            "Group name",
            duetParticipants: null);

        conversation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_WhenDuetHasNoParticipants_Throws()
    {
        var act = () => ConversationV2.Restore(
            Id<ConversationV2>.New(),
            ConversationType.Duet,
            name: null,
            duetParticipants: null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("One-on-one conversations must define their participants.*");
    }
}
