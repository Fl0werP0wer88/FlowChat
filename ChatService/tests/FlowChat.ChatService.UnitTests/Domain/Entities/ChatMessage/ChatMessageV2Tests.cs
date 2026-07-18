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
    public void Create_WhenValid_EmitsOnlyV2SentEvent()
    {
        var chatMessage = CreateMessage();

        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
        chatMessage.SequenceNum.Should().BeNull();
        var sentEvent = chatMessage.DomainEvents
            .OfType<ChatMessageSentDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        sentEvent.AggregateType.Should().Be("chat-message-v2");
        chatMessage.DomainEvents
            .OfType<ChatMessageSentDomainEvent>()
            .Should()
            .BeEmpty();
        chatMessage.DomainEvents.Should().ContainSingle();
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
            sequenceNum: null,
            DeliveryStatus.Pending,
            deliveredAtUtc: null);

        chatMessage.DomainEvents.Should().BeEmpty();
    }

    private static ChatMessageV2 CreateMessage()
    {
        return ChatMessageV2.Create(
            Id<ChatMessageV2>.New(),
            Id<ConversationV2>.New(),
            Id<UserProfileMarker>.New(),
            "Hello");
    }
}
