using FlowChat.ChatService.Domain.Entities.ChatMessage;
using FlowChat.ChatService.Domain.Entities.ChatMessage.Events;
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
    public void SetSequenceNumber_WhenNotSequenced_SetsSequenceNumber()
    {
        var chatMessage = CreateMessage();

        chatMessage.SetSequenceNumber(42);

        chatMessage.SequenceNum.Should().Be(42);
        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Pending);
        chatMessage.DeliveredAtUtc.Should().BeNull();
        var domainEvent = chatMessage.DomainEvents.OfType<ChatMessageSequencedDomainEvent>().Should().ContainSingle().Subject;
        domainEvent.MessageId.Should().Be(chatMessage.Id);
        domainEvent.SequenceNum.Should().Be(42);
    }

    [Fact]
    public void MarkAsDelivered_WhenSequencedAndNotDelivered_SetsStatusAndDeliveredAtUtc()
    {
        var chatMessage = CreateMessage();
        var deliveredAtUtc = UtcDateTimeOffset.Create(new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero));

        chatMessage.SetSequenceNumber(42);
        chatMessage.MarkAsDelivered(deliveredAtUtc);

        chatMessage.SequenceNum.Should().Be(42);
        chatMessage.DeliveryStatus.Should().Be(DeliveryStatus.Delivered);
        chatMessage.DeliveredAtUtc.Should().Be(deliveredAtUtc);
    }

    [Fact]
    public void MarkAsDelivered_WhenSequenceNumberIsMissing_ThrowsInvalidOperationException()
    {
        var chatMessage = CreateMessage();
        var deliveredAtUtc = UtcDateTimeOffset.Create(new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero));

        var act = () => chatMessage.MarkAsDelivered(deliveredAtUtc);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sequence number*");
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
