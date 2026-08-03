using System.Diagnostics.CodeAnalysis;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.Shared.Application;
using FlowChat.Shared.Consumers.Projections.Single;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.PresenceService.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionSubscriber(
    IMediator mediator,
    IConsumedOffsetCommitter consumedOffsetCommitter,
    ILogger<ContactObserverProjectionSubscriber> logger)
    : ProjectionSingleSubscriberBase<ConversationParticipantReadModelV2, ContactObserverProjectionDto>(
        mediator,
        consumedOffsetCommitter,
        logger)
{
    private const int DuetConversationType = 1;
    private const int GroupConversationType = 2;
    private const string ProjectionSource = "chat-conversation-participant-v2";

    protected override bool TryMapValue(
        ProjectionIntegrationEvent<ConversationParticipantReadModelV2> message,
        [NotNullWhen(true)] out ContactObserverProjectionDto? value)
    {
        if (message.Value is null)
            throw new NonTransientException("Payload does not contain a participant projection.");

        if (message.Operation == OperationType.Deleted && message.SourceAggregateDeletedAt is null)
            throw new NonTransientException("Deleted projection does not contain SourceAggregateDeletedAt.");

        var participantId = ResolveUserId(message.Value.ParticipantId, nameof(message.Value.ParticipantId));
        ResolveUserId(message.Value.ConversationId, nameof(message.Value.ConversationId));
        var userId = ResolveUserId(message.Value.UserId, nameof(message.Value.UserId));

        if (message.SourceAggregateId != participantId)
            throw new NonTransientException("SourceAggregateId does not match ParticipantId.");

        if (message.Value.ConversationType == GroupConversationType)
        {
            if (message.Value.DuetPartnerUserId is not null)
                throw new NonTransientException("A group participant cannot contain DuetPartnerUserId.");

            value = null;
            return false;
        }

        if (message.Value.ConversationType != DuetConversationType)
            throw new NonTransientException("ConversationType must be Duet or Group.");

        if (message.Value.DuetPartnerUserId is not Guid duetPartnerUserId)
            throw new NonTransientException("A duet participant must contain DuetPartnerUserId.");

        duetPartnerUserId = ResolveUserId(duetPartnerUserId, nameof(message.Value.DuetPartnerUserId));

        if (duetPartnerUserId == userId)
            throw new NonTransientException("DuetPartnerUserId cannot match UserId.");

        value = new ContactObserverProjectionDto
        {
            ObserverUserId = userId,
            ObservedUserId = duetPartnerUserId,
            IsBlocked = message.Value.IsBlocked,
            Source = ProjectionSource
        };
        return true;
    }

    private static Guid ResolveUserId(Guid userId, string fieldName) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");
}
