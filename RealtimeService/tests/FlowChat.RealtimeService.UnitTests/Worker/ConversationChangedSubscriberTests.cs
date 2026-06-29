using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationChanged;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConversationChangedSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ConversationChangedSubscriber _subscriber;

    public ConversationChangedSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteConversationChangedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new ConversationChangedSubscriber(
            _mediatorMock.Object,
            NullLogger<ConversationChangedSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsMappedCommandToMediator()
    {
        RouteConversationChangedCommand? capturedCommand = null;
        var conversationId = _fixture.Create<Guid>();
        var createdByUserId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteConversationChangedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RouteConversationChangedCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(
            new ConversationChangedIntegrationEvent
            {
                ConversationId = conversationId,
                Type = 2,
                Name = "Dev Team",
                CreatedByUserId = createdByUserId,
                ParticipantUserIds = [participantUserId, participantUserId, Guid.Empty]
            }.ToInboundEnvelope(),
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(conversationId);
        capturedCommand.Type.Should().Be(2);
        capturedCommand.Name.Should().Be("Dev Team");
        capturedCommand.CreatedByUserId.Should().Be(createdByUserId);
        capturedCommand.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteConversationChangedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent().ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");
    }

    private ConversationChangedIntegrationEvent CreateValidEvent() =>
        new()
        {
            ConversationId = _fixture.Create<Guid>(),
            Type = 1,
            CreatedByUserId = _fixture.Create<Guid>(),
            ParticipantUserIds = [_fixture.Create<Guid>()]
        };
}
