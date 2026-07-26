using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ChatMessageSentV2SubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ChatMessageSentV2Subscriber _subscriber;

    public ChatMessageSentV2SubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new ChatMessageSentV2Subscriber(
            _mediatorMock.Object,
            NullLogger<ChatMessageSentV2Subscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsMappedCommandToMediator()
    {
        RouteMessageCommand? capturedCommand = null;
        var messageId = _fixture.Create<Guid>();
        var conversationId = _fixture.Create<Guid>();
        var senderUserId = _fixture.Create<Guid>();
        var sentAtUtc = new DateTimeOffset(2026, 3, 17, 10, 0, 0, TimeSpan.Zero);
        const long sequenceNum = 42;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RouteMessageCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(
            new ChatMessageSentIntegrationEventV2
            {
                MessageId = messageId,
                ConversationId = conversationId,
                SenderUserId = senderUserId,
                Text = " Hi there ",
                SentAtUtc = sentAtUtc,
                SequenceNum = sequenceNum,
                ConversationMembershipRevision = 3
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.MessageId.Should().Be(messageId);
        capturedCommand.ConversationId.Should().Be(conversationId);
        capturedCommand.SenderUserId.Should().Be(senderUserId);
        capturedCommand.Text.Should().Be("Hi there");
        capturedCommand.SentAtUtc.Should().Be(sentAtUtc);
        capturedCommand.SequenceNum.Should().Be(sequenceNum);
        capturedCommand.ConversationMembershipRevision.Should().Be(3);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent().ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsTransientFailure_ThrowsTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.UnExpected(
                    "membership projection is stale",
                    FailureKind.Transient)));

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent().ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<TransientException>()
            .WithMessage("membership projection is stale");
    }

    private ChatMessageSentIntegrationEventV2 CreateValidEvent() =>
        new()
        {
            MessageId = _fixture.Create<Guid>(),
            ConversationId = _fixture.Create<Guid>(),
            SenderUserId = _fixture.Create<Guid>(),
            Text = "Hi there",
            SentAtUtc = DateTimeOffset.UtcNow,
            SequenceNum = 42,
            ConversationMembershipRevision = 2
        };
}
