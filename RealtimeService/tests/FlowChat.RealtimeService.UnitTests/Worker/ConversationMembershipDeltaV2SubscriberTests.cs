using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Unit = MediatR.Unit;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ConversationMembershipDeltaV2SubscriberTests
{
    private const int GroupConversationType = 2;
    private readonly IFixture _fixture = new Fixture();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ConversationMembershipDeltaV2Subscriber _subscriber;

    public ConversationMembershipDeltaV2SubscriberTests()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<RouteConversationMembershipDeltaV2Command>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        _subscriber = new ConversationMembershipDeltaV2Subscriber(
            _mediatorMock.Object,
            NullLogger<ConversationMembershipDeltaV2Subscriber>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ValidMixedDelta_ForwardsMappedCommandToMediator()
    {
        RouteConversationMembershipDeltaV2Command? capturedCommand = null;
        var conversationId = _fixture.Create<Guid>();
        var addedParticipantUserId = _fixture.Create<Guid>();
        var removedParticipantUserId = _fixture.Create<Guid>();
        var message = CreateEvent(
            conversationId,
            [
                CreateItem(
                    conversationId,
                    addedParticipantUserId,
                    OperationType.Created),
                CreateItem(
                    conversationId,
                    removedParticipantUserId,
                    OperationType.Deleted)
            ]);

        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<RouteConversationMembershipDeltaV2Command>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) =>
                capturedCommand = (RouteConversationMembershipDeltaV2Command)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(conversationId);
        capturedCommand.ConversationType.Should().Be(GroupConversationType);
        capturedCommand.ProjectionRevision.Should().Be(message.ProjectionRevision);
        capturedCommand.Delta.Should().Equal(
            new ConversationMembershipDeltaItemV2(
                addedParticipantUserId,
                OperationType.Created),
            new ConversationMembershipDeltaItemV2(
                removedParticipantUserId,
                OperationType.Deleted));
    }

    [Fact]
    public async Task HandleAsync_EmptyDelta_ThrowsNonTransientException()
    {
        var message = CreateEvent(
            _fixture.Create<Guid>(),
            []);

        var act = () => _subscriber.HandleAsync(
            message.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("Conversation membership delta cannot be empty.");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_MismatchedConversationId_ThrowsNonTransientException()
    {
        var conversationId = _fixture.Create<Guid>();
        var message = CreateEvent(
            conversationId,
            [
                CreateItem(
                    _fixture.Create<Guid>(),
                    _fixture.Create<Guid>(),
                    OperationType.Created)
            ]);

        var act = () => _subscriber.HandleAsync(
            message.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("All membership delta values must belong to the projected conversation.");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_UpdatedItem_ThrowsNonTransientException()
    {
        var conversationId = _fixture.Create<Guid>();
        var message = CreateEvent(
            conversationId,
            [
                CreateItem(
                    conversationId,
                    _fixture.Create<Guid>(),
                    OperationType.Updated)
            ]);

        var act = () => _subscriber.HandleAsync(
            message.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("Updating a conversation membership item is not supported.");
        VerifyCommandWasNotSent();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public async Task HandleAsync_UnsupportedConversationType_ThrowsNonTransientException(int conversationType)
    {
        var conversationId = _fixture.Create<Guid>();
        var message = CreateEvent(
            conversationId,
            [CreateItem(conversationId, _fixture.Create<Guid>(), OperationType.Created, conversationType)]);

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("All membership delta values must have the same supported conversation type.");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_MixedConversationTypes_ThrowsNonTransientException()
    {
        var conversationId = _fixture.Create<Guid>();
        var message = CreateEvent(
            conversationId,
            [
                CreateItem(conversationId, _fixture.Create<Guid>(), OperationType.Created, 1),
                CreateItem(conversationId, _fixture.Create<Guid>(), OperationType.Created, 2)
            ]);

        var act = () => _subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("All membership delta values must have the same supported conversation type.");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_DuplicateParticipant_ThrowsNonTransientException()
    {
        var conversationId = _fixture.Create<Guid>();
        var participantUserId = _fixture.Create<Guid>();
        var message = CreateEvent(
            conversationId,
            [
                CreateItem(
                    conversationId,
                    participantUserId,
                    OperationType.Created),
                CreateItem(
                    conversationId,
                    participantUserId,
                    OperationType.Deleted)
            ]);

        var act = () => _subscriber.HandleAsync(
            message.ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage($"Conversation membership delta contains duplicate participant '{participantUserId}'.");
        VerifyCommandWasNotSent();
    }

    [Fact]
    public async Task HandleAsync_WhenCommandReturnsFailure_ThrowsNonTransientException()
    {
        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<RouteConversationMembershipDeltaV2Command>(),
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
                It.IsAny<RouteConversationMembershipDeltaV2Command>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(
                DomainError.UnExpected("projection revision gap", FailureKind.Transient)));

        var act = () => _subscriber.HandleAsync(
            CreateValidEvent().ToInboundEnvelope(),
            CancellationToken.None);

        await act.Should().ThrowAsync<TransientException>()
            .WithMessage("projection revision gap");
    }

    private DeltaProjectionIntegrationEventV2<ConversationMembershipReadModelV2>
        CreateValidEvent()
    {
        var conversationId = _fixture.Create<Guid>();
        return CreateEvent(
            conversationId,
            [
                CreateItem(
                    conversationId,
                    _fixture.Create<Guid>(),
                    OperationType.Created)
            ]);
    }

    private static DeltaProjectionIntegrationEventV2<ConversationMembershipReadModelV2>
        CreateEvent(
            Guid conversationId,
            IReadOnlyList<DeltaProjectionItemV2<ConversationMembershipReadModelV2>> delta) =>
        new()
        {
            ProjectionId = conversationId,
            ProjectionRevision = 2,
            Delta = delta
        };

    private DeltaProjectionItemV2<ConversationMembershipReadModelV2> CreateItem(
        Guid conversationId,
        Guid participantUserId,
        OperationType operation,
        int conversationType = GroupConversationType) =>
        new()
        {
            SourceAggregateId = _fixture.Create<Guid>(),
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateVersion = 2,
            Operation = operation,
            Value = new ConversationMembershipReadModelV2
            {
                ConversationId = conversationId,
                ConversationType = conversationType,
                ParticipantUserId = participantUserId
            }
        };

    private void VerifyCommandWasNotSent()
    {
        _mediatorMock.Verify(
            x => x.Send(
                It.IsAny<RouteConversationMembershipDeltaV2Command>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
