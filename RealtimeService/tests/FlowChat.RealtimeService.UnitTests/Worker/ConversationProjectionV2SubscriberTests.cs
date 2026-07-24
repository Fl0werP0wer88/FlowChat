using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConversationProjectionV2SubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ConversationProjectionV2Subscriber _subscriber;

    public ConversationProjectionV2SubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<RouteConversationProjectionV2Command>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new ConversationProjectionV2Subscriber(
            _mediatorMock.Object,
            NullLogger<ConversationProjectionV2Subscriber>.Instance);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task HandleAsync_ValidProjection_ForwardsMappedCommandToMediator(
        int conversationType)
    {
        RouteConversationProjectionV2Command? capturedCommand = null;
        var message = CreateValidEvent(conversationType);

        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<RouteConversationProjectionV2Command>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RouteConversationProjectionV2Command)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.SourceAggregateId.Should().Be(message.SourceAggregateId);
        capturedCommand.ConversationId.Should().Be(message.Value.ConversationId);
        capturedCommand.ConversationType.Should().Be(conversationType);
        capturedCommand.Name.Should().Be(message.Value.Name);
        capturedCommand.CreatedByUserId.Should().Be(message.Value.CreatedByUserId);
        capturedCommand.Operation.Should().Be(message.Operation);
    }

    [Fact]
    public async Task HandleAsync_NullProjection_ThrowsNonTransientException()
    {
        var message = CreateValidEvent() with { Value = null! };

        var act = () => _subscriber.HandleAsync(
            message.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("Conversation projection value cannot be null.");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_MismatchedConversationId_ThrowsNonTransientException()
    {
        var message = CreateValidEvent() with
        {
            SourceAggregateId = _fixture.Create<Guid>()
        };

        var act = () => _subscriber.HandleAsync(
            message.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("Conversation projection must belong to the source conversation aggregate.");
        VerifyCommandWasNotSent();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task HandleAsync_UnsupportedConversationType_ThrowsNonTransientException(
        int conversationType)
    {
        var message = CreateValidEvent(conversationType);

        var act = () => _subscriber.HandleAsync(
            message.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage($"Unsupported conversation type '{conversationType}'.");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<RouteConversationProjectionV2Command>(),
                It.IsAny<CancellationToken>()))
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
            .Setup(x => x.Send(
                It.IsAny<RouteConversationProjectionV2Command>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.UnExpected("membership is unavailable", FailureKind.Transient)));

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent().ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<TransientException>()
            .WithMessage("membership is unavailable");
    }

    private ProjectionIntegrationEvent<ConversationReadModelV2> CreateValidEvent(
        int conversationType = 1)
    {
        var conversationId = _fixture.Create<Guid>();

        return new ProjectionIntegrationEvent<ConversationReadModelV2>
        {
            SourceAggregateId = conversationId,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = OperationType.Created,
            SourceAggregateVersion = 2,
            Value = new ConversationReadModelV2
            {
                ConversationId = conversationId,
                ConversationType = conversationType,
                Name = "Dev Team",
                CreatedByUserId = _fixture.Create<Guid>()
            }
        };
    }

    private void VerifyCommandWasNotSent()
    {
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<RouteConversationProjectionV2Command>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
