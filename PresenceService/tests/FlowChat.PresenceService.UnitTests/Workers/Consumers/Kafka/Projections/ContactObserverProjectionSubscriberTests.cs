using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Core.Results;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Consumers.Kafka.Projections;
using FlowChat.Shared.Application;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.PresenceService.UnitTests.Workers.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionSubscriberTests
{
    private const int DuetConversationType = 1;
    private const int GroupConversationType = 2;

    [Theory]
    [InlineData(OperationType.Created)]
    [InlineData(OperationType.Updated)]
    [InlineData(OperationType.Deleted)]
    public async Task HandleAsync_ValidDuetParticipant_SendsProjectionCommand(OperationType operation)
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var message = CreateProjectionEvent(
            operation: operation,
            userId: userId,
            duetPartnerUserId: partnerUserId,
            isBlocked: true);

        var command = await HandleAndCaptureCommandAsync(message);

        command.Item.Value.ObserverUserId.Should().Be(userId);
        command.Item.Value.ObservedUserId.Should().Be(partnerUserId);
        command.Item.Value.IsBlocked.Should().BeTrue();
        command.Item.Value.Source.Should().Be("chat-conversation-participant-v2");
        command.Item.Operation.Should().Be(operation);
        command.Item.SourceDeletedAtUtc.Should().Be(message.SourceAggregateDeletedAt);
    }

    [Fact]
    public async Task HandleAsync_GroupParticipant_CommitsOffsetWithoutSendingCommand()
    {
        var mediatorMock = new Mock<IMediator>();
        var offsetCommitterMock = new Mock<IConsumedOffsetCommitter>();
        offsetCommitterMock
            .Setup(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var subscriber = CreateSubscriber(mediatorMock, offsetCommitterMock);
        var message = CreateProjectionEvent(
            conversationType: GroupConversationType,
            omitDuetPartner: true);

        await subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        offsetCommitterMock.Verify(
            x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        mediatorMock.Verify(
            x => x.Send(
                It.IsAny<ProjectionSingleCommand<ContactObserverProjectionDto>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_GroupParticipantWhenOffsetCommitFails_PropagatesException()
    {
        var offsetCommitterMock = new Mock<IConsumedOffsetCommitter>();
        offsetCommitterMock
            .Setup(x => x.CommitConsumedOffsetsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TransientException("offset failed"));
        var subscriber = CreateSubscriber(offsetCommitterMock: offsetCommitterMock);
        var message = CreateProjectionEvent(
            conversationType: GroupConversationType,
            omitDuetPartner: true);

        var action = () => subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await action.Should().ThrowAsync<TransientException>().WithMessage("offset failed");
    }

    [Fact]
    public async Task HandleAsync_MissingValue_ThrowsNonTransientException()
    {
        var message = CreateProjectionEvent() with { Value = null! };

        await AssertNonTransientAsync(message);
    }

    [Fact]
    public async Task HandleAsync_InvalidSourceVersion_ThrowsNonTransientException()
    {
        var message = CreateProjectionEvent() with { SourceAggregateVersion = 0 };

        await AssertNonTransientAsync(message);
    }

    [Fact]
    public async Task HandleAsync_DeletedWithoutTimestamp_ThrowsNonTransientException()
    {
        var message = CreateProjectionEvent(operation: OperationType.Deleted) with
        {
            SourceAggregateDeletedAt = null
        };

        await AssertNonTransientAsync(message);
    }

    [Fact]
    public async Task HandleAsync_SourceAggregateIdMismatch_ThrowsNonTransientException()
    {
        var message = CreateProjectionEvent() with { SourceAggregateId = Guid.NewGuid() };

        await AssertNonTransientAsync(message);
    }

    [Theory]
    [InlineData(999, false)]
    [InlineData(DuetConversationType, true)]
    public async Task HandleAsync_InvalidConversationShape_ThrowsNonTransientException(
        int conversationType,
        bool omitDuetPartner)
    {
        var message = CreateProjectionEvent(
            conversationType: conversationType,
            omitDuetPartner: omitDuetPartner);

        await AssertNonTransientAsync(message);
    }

    [Fact]
    public async Task HandleAsync_GroupWithDuetPartner_ThrowsNonTransientException()
    {
        var message = CreateProjectionEvent(
            conversationType: GroupConversationType,
            duetPartnerUserId: Guid.NewGuid());

        await AssertNonTransientAsync(message);
    }

    [Fact]
    public async Task HandleAsync_DuetWithSelfAsPartner_ThrowsNonTransientException()
    {
        var userId = Guid.NewGuid();
        var message = CreateProjectionEvent(userId: userId, duetPartnerUserId: userId);

        await AssertNonTransientAsync(message);
    }

    [Theory]
    [InlineData("participant")]
    [InlineData("conversation")]
    [InlineData("user")]
    [InlineData("partner")]
    public async Task HandleAsync_EmptyRequiredIdentifier_ThrowsNonTransientException(string field)
    {
        var message = CreateProjectionEvent(
            participantId: field == "participant" ? Guid.Empty : null,
            conversationId: field == "conversation" ? Guid.Empty : null,
            userId: field == "user" ? Guid.Empty : null,
            duetPartnerUserId: field == "partner" ? Guid.Empty : null);

        await AssertNonTransientAsync(message);
    }

    private static async Task<ProjectionSingleCommand<ContactObserverProjectionDto>> HandleAndCaptureCommandAsync(
        ProjectionIntegrationEvent<ConversationParticipantReadModelV2> message)
    {
        ProjectionSingleCommand<ContactObserverProjectionDto>? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(
                It.IsAny<ProjectionSingleCommand<ContactObserverProjectionDto>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((command, _) =>
                capturedCommand = (ProjectionSingleCommand<ContactObserverProjectionDto>)command)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        await CreateSubscriber(mediatorMock).HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        return capturedCommand!;
    }

    private static async Task AssertNonTransientAsync(
        ProjectionIntegrationEvent<ConversationParticipantReadModelV2> message)
    {
        var subscriber = CreateSubscriber();

        var action = () => subscriber.HandleAsync(message.ToInboundEnvelope(), CancellationToken.None);

        await action.Should().ThrowAsync<NonTransientException>();
    }

    private static ContactObserverProjectionSubscriber CreateSubscriber(
        Mock<IMediator>? mediatorMock = null,
        Mock<IConsumedOffsetCommitter>? offsetCommitterMock = null) =>
        new(
            (mediatorMock ?? new Mock<IMediator>()).Object,
            (offsetCommitterMock ?? new Mock<IConsumedOffsetCommitter>()).Object,
            NullLogger<ContactObserverProjectionSubscriber>.Instance);

    private static ProjectionIntegrationEvent<ConversationParticipantReadModelV2> CreateProjectionEvent(
        Guid? participantId = null,
        Guid? conversationId = null,
        int conversationType = DuetConversationType,
        Guid? userId = null,
        Guid? duetPartnerUserId = null,
        bool omitDuetPartner = false,
        bool isBlocked = false,
        OperationType operation = OperationType.Updated)
    {
        var resolvedParticipantId = participantId ?? Guid.NewGuid();

        return new ProjectionIntegrationEvent<ConversationParticipantReadModelV2>
        {
            SourceAggregateId = resolvedParticipantId,
            SourceAggregateCreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            SourceAggregateModifiedAtUtc = DateTimeOffset.UtcNow,
            SourceAggregateDeletedAt = operation == OperationType.Deleted ? DateTimeOffset.UtcNow : null,
            Operation = operation,
            SourceAggregateVersion = 2,
            Value = new ConversationParticipantReadModelV2
            {
                ParticipantId = resolvedParticipantId,
                ConversationId = conversationId ?? Guid.NewGuid(),
                ConversationType = conversationType,
                UserId = userId ?? Guid.NewGuid(),
                DuetPartnerUserId = omitDuetPartner ? null : duetPartnerUserId ?? Guid.NewGuid(),
                IsBlocked = isBlocked,
                IsMuted = false,
                IsHidden = false,
                JoinedAtUtc = DateTimeOffset.UtcNow,
                LastReadMessageSequenceNum = 0
            }
        };
    }
}
