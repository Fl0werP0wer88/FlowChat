using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class ConversationMembershipDeltaV2Subscriber(
    IMediator mediator,
    ILogger<ConversationMembershipDeltaV2Subscriber> logger)
    : SubscriberBase<DeltaProjectionIntegrationEventV2<ConversationMembershipReadModelV2>>(logger)
{
    protected override async Task ExecuteAsync(
        DeltaProjectionIntegrationEventV2<ConversationMembershipReadModelV2> message,
        CancellationToken cancellationToken)
    {
        if (message.Delta is null || message.Delta.Count == 0)
        {
            throw new NonTransientException("Conversation membership delta cannot be empty.");
        }

        if (message.Delta.Any(item =>
                item is null ||
                item.Value is null ||
                item.Value.ConversationId != message.ProjectionId))
        {
            throw new NonTransientException(
                "All membership delta values must belong to the projected conversation.");
        }

        if (message.Delta.Any(item => item.Operation == OperationType.Updated))
        {
            throw new NonTransientException(
                "Updating a conversation membership item is not supported.");
        }

        var duplicateParticipantUserId = message.Delta
            .GroupBy(item => item.Value.ParticipantUserId)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateParticipantUserId.HasValue)
        {
            throw new NonTransientException(
                $"Conversation membership delta contains duplicate participant '{duplicateParticipantUserId}'.");
        }

        var command = new RouteConversationMembershipDeltaV2Command(
            message.ProjectionId,
            message.ProjectionRevision,
            message.Delta
                .Select(item => new ConversationMembershipDeltaItemV2(
                    item.Value.ParticipantUserId,
                    item.Operation))
                .ToArray());

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
