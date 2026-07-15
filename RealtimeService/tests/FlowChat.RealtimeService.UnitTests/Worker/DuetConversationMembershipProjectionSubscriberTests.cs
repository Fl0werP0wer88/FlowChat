using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteDuetConversationCreated;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class DuetConversationMembershipProjectionSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DuetConversationMembershipProjectionSubscriber _subscriber;

    public DuetConversationMembershipProjectionSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteDuetConversationCreatedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new DuetConversationMembershipProjectionSubscriber(
            _mediatorMock.Object,
            NullLogger<DuetConversationMembershipProjectionSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ForwardsMembershipStateToMediator()
    {
        RouteDuetConversationCreatedCommand? capturedCommand = null;
        var message = CreateValidEvent();

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteDuetConversationCreatedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RouteDuetConversationCreatedCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(message.Value.ConversationId);
        capturedCommand.ParticipantUserIds.Should().BeEquivalentTo(
            new[] { message.Value.FirstUserId, message.Value.SecondUserId });
        capturedCommand.ConversationMembershipRevision.Should().Be(message.Value.ConversationMembershipRevision);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteDuetConversationCreatedCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var act = () => _subscriber.HandleAsync(CreateValidEvent().ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("boom");
    }

    private ProjectionIntegrationEvent<DuetConversationMembershipReadModel> CreateValidEvent() =>
        new()
        {
            SourceAggregateId = _fixture.Create<Guid>(),
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = OperationType.Created,
            SourceAggregateVersion = 1,
            Value = new DuetConversationMembershipReadModel
            {
                ConversationId = _fixture.Create<Guid>(),
                FirstUserId = _fixture.Create<Guid>(),
                SecondUserId = _fixture.Create<Guid>(),
                ConversationMembershipRevision = 1
            }
        };
}
