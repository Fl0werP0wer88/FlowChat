using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationMembershipDelta;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class GroupConversationMembershipProjectionSubscriberTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly GroupConversationMembershipProjectionSubscriber _subscriber;

    public GroupConversationMembershipProjectionSubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteGroupConversationMembershipDeltaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new GroupConversationMembershipProjectionSubscriber(
            _mediatorMock.Object,
            NullLogger<GroupConversationMembershipProjectionSubscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ValidDelta_ForwardsProjectionToMediator()
    {
        RouteGroupConversationMembershipDeltaCommand? capturedCommand = null;
        var message = CreateValidEvent();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteGroupConversationMembershipDeltaCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RouteGroupConversationMembershipDeltaCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(message.SourceAggregateId);
        capturedCommand.ParticipantUserIds.Should().Equal(message.Value.Select(value => value.ParticipantUserId));
        capturedCommand.Operation.Should().Be(message.Operation);
        capturedCommand.ProjectionRevision.Should().Be(message.ProjectionRevision);
    }

    [Fact]
    public async Task HandleAsync_MismatchedConversationId_ThrowsNonTransientException()
    {
        var message = CreateValidEvent() with
        {
            Value =
            [
                new GroupConversationMembershipReadModel
                {
                    ConversationId = _fixture.Create<Guid>(),
                    ParticipantUserId = _fixture.Create<Guid>()
                }
            ]
        };

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("All membership values must belong to the source group conversation.");
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<RouteGroupConversationMembershipDeltaCommand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CommandFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteGroupConversationMembershipDeltaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.BadRequest("boom")));

        var act = () => _subscriber.HandleAsync(CreateValidEvent().ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>().WithMessage("boom");
    }

    private DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel> CreateValidEvent()
    {
        var conversationId = _fixture.Create<Guid>();
        return new DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>
        {
            SourceAggregateId = conversationId,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            Operation = DeltaOperationType.Added,
            SourceAggregateVersion = 1,
            ProjectionRevision = 1,
            Value =
            [
                new GroupConversationMembershipReadModel
                {
                    ConversationId = conversationId,
                    ParticipantUserId = _fixture.Create<Guid>()
                }
            ]
        };
    }
}
