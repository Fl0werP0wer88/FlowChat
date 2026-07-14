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

public sealed class ChatMessageSentSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ChatMessageSentSubscriber _subscriber;

    public ChatMessageSentSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new ChatMessageSentSubscriber(
            _mediatorMock.Object,
            NullLogger<ChatMessageSentSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsMappedCommandToMediator()
    {
        RouteMessageCommand? capturedCommand = null;
        var recipientUserId = _fixture.Create<Guid>();
        var messageId = _fixture.Create<Guid>();
        var conversationId = _fixture.Create<Guid>();
        var senderUserId = _fixture.Create<Guid>();
        var sentAtUtc = new DateTimeOffset(2026, 3, 17, 10, 0, 0, TimeSpan.Zero);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RouteMessageCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(
            new ChatMessageSentIntegrationEvent
            {
                MessageId = messageId,
                ConversationId = conversationId,
                SenderUserId = senderUserId,
                Text = " Hi there ",
                SentAtUtc = sentAtUtc,
                RecipientUserIds = [recipientUserId, recipientUserId, Guid.Empty]
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.MessageId.Should().Be(messageId);
        capturedCommand.ConversationId.Should().Be(conversationId);
        capturedCommand.SenderUserId.Should().Be(senderUserId);
        capturedCommand.Text.Should().Be("Hi there");
        capturedCommand.SentAtUtc.Should().Be(sentAtUtc);
        capturedCommand.RecipientUserIds.Should().ContainSingle().Which.Should().Be(recipientUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenMessageIdMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("MessageId is required.");

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(messageId: Guid.Empty).ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*MessageId*");
        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenConversationIdMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("ConversationId is required.");

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(conversationId: Guid.Empty).ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*ConversationId*");
        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenSenderUserIdMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("SenderUserId is required.");

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(senderUserId: Guid.Empty).ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*SenderUserId*");
        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenTextMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("Text is required.");

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(text: " ").ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*Text*");
        VerifyCommandWasSent();
    }

    [Fact]
    public async Task HandleAsync_WhenRecipientUserIdsMissing_ThrowsNonTransientException()
    {
        SetupCommandFailure("RecipientUserIds must contain at least one valid user id.");

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent(recipientUserIds: [Guid.Empty]).ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*RecipientUserIds*");
        VerifyCommandWasSent();
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

    private ChatMessageSentIntegrationEvent CreateValidEvent(
        Guid? messageId = null,
        Guid? conversationId = null,
        Guid? senderUserId = null,
        string text = "Hi there",
        IReadOnlyCollection<Guid>? recipientUserIds = null) =>
        new()
        {
            MessageId = messageId ?? _fixture.Create<Guid>(),
            ConversationId = conversationId ?? _fixture.Create<Guid>(),
            SenderUserId = senderUserId ?? _fixture.Create<Guid>(),
            Text = text,
            SentAtUtc = DateTimeOffset.UtcNow,
            RecipientUserIds = (recipientUserIds ?? [_fixture.Create<Guid>()]).ToList()
        };

    private void SetupCommandFailure(string errorMessage)
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest(errorMessage)));
    }

    private void VerifyCommandWasSent()
    {
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
