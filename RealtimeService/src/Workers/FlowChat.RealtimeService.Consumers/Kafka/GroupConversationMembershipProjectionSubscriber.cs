using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationMembershipDelta;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class GroupConversationMembershipProjectionSubscriber(
    IMediator mediator,
    ILogger<GroupConversationMembershipProjectionSubscriber> logger)
    : SubscriberBase<DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel>>(logger)
{
    protected override async Task ExecuteAsync(
        DeltaProjectionIntegrationEvent<GroupConversationMembershipReadModel> message,
        CancellationToken cancellationToken)
    {
        var values = (message.Value ?? []).ToArray();
        if (values.Any(value => value.ConversationId != message.SourceAggregateId))
        {
            throw new NonTransientException("All membership values must belong to the source group conversation.");
        }

        var command = new RouteGroupConversationMembershipDeltaCommand(
            message.SourceAggregateId,
            values.Select(value => value.ParticipantUserId).ToArray(),
            message.Operation,
            message.ProjectionRevision);

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
