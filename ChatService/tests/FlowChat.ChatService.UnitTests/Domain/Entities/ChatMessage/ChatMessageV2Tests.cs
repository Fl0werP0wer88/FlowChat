using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ChatMessage;

public sealed class ChatMessageV2Tests
{
    [Fact]
    public void Create_WhenValid_EmitsOnlyV2SentEventWithRecipients()
    {
        var recipientUserIds = CreateUserIds(2);

        var chatMessage = CreateMessage(recipientUserIds);

        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
        chatMessage.SequenceNum.Should().BeNull();
        var sentEvent = chatMessage.DomainEvents
            .OfType<ChatMessageSentDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        sentEvent.RecipientUserIds.Should().Equal(recipientUserIds);
        sentEvent.AggregateType.Should().Be("chat-message-v2");
        chatMessage.DomainEvents
            .OfType<ChatMessageSentDomainEvent>()
            .Should()
            .BeEmpty();
        chatMessage.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Create_WhenRecipientsContainDuplicates_NormalizesAggregateAndEventRecipients()
    {
        var firstRecipientUserId = Id<UserProfileMarker>.New();
        var secondRecipientUserId = Id<UserProfileMarker>.New();

        var chatMessage = CreateMessage(
            [firstRecipientUserId, secondRecipientUserId, firstRecipientUserId]);

        chatMessage.RecipientUserIds.Should().Equal(
            firstRecipientUserId,
            secondRecipientUserId);
        chatMessage.DomainEvents
            .OfType<ChatMessageSentDomainEventV2>()
            .Single()
            .RecipientUserIds.Should()
            .Equal(firstRecipientUserId, secondRecipientUserId);
    }

    [Fact]
    public void SetSequenceNumber_WhenCalledTwice_DoesNotReplaceAssignedNumber()
    {
        var chatMessage = CreateMessage();

        chatMessage.SetSequenceNumber(42).Should().BeTrue();
        chatMessage.SetSequenceNumber(43).Should().BeFalse();

        chatMessage.SequenceNum.Should().Be(42);
    }

    [Fact]
    public void MarkAsDelivered_WhenSequenced_IsIdempotent()
    {
        var chatMessage = CreateMessage();
        var deliveredAtUtc = UtcDateTimeOffset.Create(
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));
        chatMessage.SetSequenceNumber(42);

        chatMessage.MarkAsDelivered(deliveredAtUtc).Should().BeTrue();
        chatMessage.MarkAsDelivered(deliveredAtUtc).Should().BeFalse();

        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
        chatMessage.DeliveredAtUtc.Should().Be(deliveredAtUtc);
    }

    [Fact]
    public void Restore_WhenCalled_DoesNotEmitDomainEvents()
    {
        var chatMessage = ChatMessageV2.Restore(
            Id<ChatMessageV2>.New(),
            Id<ConversationV2>.New(),
            Id<UserProfileMarker>.New(),
            "Hello",
            UtcDateTimeOffset.UtcNow,
            CreateUserIds(1),
            sequenceNum: null,
            DeliveryStatus.Pending,
            deliveredAtUtc: null);

        chatMessage.DomainEvents.Should().BeEmpty();
    }

    private static ChatMessageV2 CreateMessage(
        IReadOnlyCollection<Id<UserProfileMarker>>? recipientUserIds = null)
    {
        return ChatMessageV2.Create(
            Id<ChatMessageV2>.New(),
            Id<ConversationV2>.New(),
            Id<UserProfileMarker>.New(),
            "Hello",
            recipientUserIds ?? CreateUserIds(1));
    }

    private static IReadOnlyCollection<Id<UserProfileMarker>> CreateUserIds(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Id<UserProfileMarker>.New())
            .ToArray();
}
