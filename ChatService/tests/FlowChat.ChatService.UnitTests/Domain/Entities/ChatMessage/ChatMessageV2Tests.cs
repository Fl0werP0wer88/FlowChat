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
    public void Create_WhenValid_EmitsSentEvent()
    {
        var chatMessage = CreateMessage();

        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
        chatMessage.SequenceNum.Should().Be(42);
        var sentEvent = chatMessage.DomainEvents
            .OfType<ChatMessageSentDomainEventV2>()
            .Should()
            .ContainSingle()
            .Subject;
        sentEvent.AggregateType.Should().Be("chat-message-v2");
        sentEvent.SequenceNum.Should().Be(42);
        chatMessage.DomainEvents.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WhenSequenceNumberIsNotPositive_Throws(long sequenceNum)
    {
        var action = () => ChatMessageV2.Create(
            Id<ChatMessageV2>.New(),
            Id<ConversationV2>.New(),
            Id<UserProfileMarker>.New(),
            "Hello",
            sequenceNum);

        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkAsDelivered_WhenSequenced_IsIdempotent()
    {
        var chatMessage = CreateMessage();
        var deliveredAtUtc = UtcDateTimeOffset.Create(
            new DateTimeOffset(2026, 7, 19, 12, 0, 0, TimeSpan.Zero));

        chatMessage.MarkAsDelivered(deliveredAtUtc).Should().BeTrue();
        chatMessage.MarkAsDelivered(deliveredAtUtc).Should().BeFalse();

        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
        chatMessage.DeliveredAtUtc.Should().Be(deliveredAtUtc);
        chatMessage.SequenceNum.Should().Be(42);
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
            sequenceNum: 42,
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
            "Hello",
            sequenceNum: 42);
    }
}
