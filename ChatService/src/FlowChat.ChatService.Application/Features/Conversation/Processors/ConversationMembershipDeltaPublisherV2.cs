using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using UserProfileMarker = FlowChat.ChatService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.ChatService.Application.Features.Conversation.Processors;

public sealed class ConversationMembershipDeltaPublisherV2(
    IOutboxIntegrationEventPublisher publisher)
{
    public Task PublishAsync(
        ConversationMembership membership,
        IEnumerable<Id<UserProfileMarker>> participantUserIds,
        DeltaOperationType operation,
        CancellationToken cancellationToken)
    {
        var values = participantUserIds
            .Select(userId => new ConversationMembershipReadModelV2
            {
                ConversationId = membership.ConversationId.Value,
                ParticipantUserId = userId.Value
            })
            .ToArray();

        if (values.Length == 0)
            return Task.CompletedTask;

        var integrationEvent = new DeltaProjectionIntegrationEvent<ConversationMembershipReadModelV2>
        {
            SourceAggregateId = membership.Id.Value,
            SourceAggregateCreatedAtUtc = membership.CreatedAtUtc.Value,
            SourceAggregateModifiedAtUtc = membership.LastModifiedAtUtc.Value,
            SourceAggregateDeletedAt = membership.DeletedAt?.Value,
            Value = values,
            Operation = operation,
            SourceAggregateVersion = membership.Version,
            ProjectionRevision = membership.Version
        };

        return publisher.PublishAsync(
            integrationEvent,
            membership.ConversationId.Value.ToString("D"),
            cancellationToken);
    }
}
