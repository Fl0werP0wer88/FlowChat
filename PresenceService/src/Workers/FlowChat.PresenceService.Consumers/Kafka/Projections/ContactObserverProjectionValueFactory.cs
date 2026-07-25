using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.Shared.Consumers.ProjectionBulk;

namespace FlowChat.PresenceService.Consumers.Kafka.Projections;

public sealed class ContactObserverProjectionValueFactory
    : IProjectionValueFactory<ConversationParticipantReadModelV2, ContactObserverProjectionDto, (Guid ObservedUserId, Guid ObserverUserId)>
{
    private const int DuetConversationType = 1;
    private const int GroupConversationType = 2;
    private const string ProjectionSource = "chat-conversation-participant-v2";

    public ContactObserverProjectionDto MapValue(ProjectionIntegrationEvent<ConversationParticipantReadModelV2> message) =>
        MapValues(message).Single();

    public IEnumerable<ContactObserverProjectionDto> MapValues(
        ProjectionIntegrationEvent<ConversationParticipantReadModelV2> message)
    {
        if (message.Value is null)
            throw new NonTransientException("Payload does not contain a participant projection.");

        var participantId = ResolveUserId(message.Value.ParticipantId, nameof(message.Value.ParticipantId));
        ResolveUserId(message.Value.ConversationId, nameof(message.Value.ConversationId));
        var userId = ResolveUserId(message.Value.UserId, nameof(message.Value.UserId));

        if (message.SourceAggregateId != participantId)
            throw new NonTransientException("SourceAggregateId does not match ParticipantId.");

        if (message.Value.ConversationType == GroupConversationType)
        {
            if (message.Value.DuetPartnerUserId is not null)
                throw new NonTransientException("A group participant cannot contain DuetPartnerUserId.");

            return [];
        }

        if (message.Value.ConversationType != DuetConversationType)
            throw new NonTransientException("ConversationType must be Duet or Group.");

        if (message.Value.DuetPartnerUserId is not Guid duetPartnerUserId)
            throw new NonTransientException("A duet participant must contain DuetPartnerUserId.");

        duetPartnerUserId = ResolveUserId(duetPartnerUserId, nameof(message.Value.DuetPartnerUserId));

        if (duetPartnerUserId == userId)
            throw new NonTransientException("DuetPartnerUserId cannot match UserId.");

        return
        [
            new ContactObserverProjectionDto
            {
                ObserverUserId = userId,
                ObservedUserId = duetPartnerUserId,
                IsBlocked = message.Value.IsBlocked,
                Source = ProjectionSource
            }
        ];
    }

    public (Guid ObservedUserId, Guid ObserverUserId) GetDeduplicationKey(ContactObserverProjectionDto value) =>
        (value.ObservedUserId, value.ObserverUserId);

    private static Guid ResolveUserId(Guid userId, string fieldName) =>
        userId != Guid.Empty
            ? userId
            : throw new NonTransientException($"Payload does not contain valid {fieldName}.");
}
