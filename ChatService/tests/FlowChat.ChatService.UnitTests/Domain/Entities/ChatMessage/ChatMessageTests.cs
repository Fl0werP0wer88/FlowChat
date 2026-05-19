using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using ChatMessageAggregate = FlowChat.ChatService.Domain.Entities.ChatMessage.ChatMessage;

namespace FlowChat.ChatService.UnitTests.Domain.Entities.ChatMessage;

public sealed class ChatMessageTests
{
    [Fact]
    public void Create_WhenValidMessage_DeliveryStateIsPending()
    {
        var chatMessage = CreateMessage();

        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
        chatMessage.SequenceNum.Should().BeNull();
        chatMessage.DeliveredAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkAsDelivered_WhenNotDelivered_SetsSequenceStatusAndDeliveredAtUtc()
    {
        var chatMessage = CreateMessage();
        var deliveredAtUtc = UtcDateTimeOffset.Create(new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero));

        chatMessage.MarkAsDelivered(42, deliveredAtUtc);

        chatMessage.SequenceNum.Should().Be(42);
        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
        chatMessage.DeliveredAtUtc.Should().Be(deliveredAtUtc);
    }

    private static ChatMessageAggregate CreateMessage() =>
        ChatMessageAggregate.Create(
            Id<ChatMessageAggregate>.New(),
            Id<Conversation>.New(),
            Guid.NewGuid(),
            "Alice",
            "Hello",
            [Guid.NewGuid()]);
}
