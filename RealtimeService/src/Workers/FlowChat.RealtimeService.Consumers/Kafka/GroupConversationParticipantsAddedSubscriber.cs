using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsAdded;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class GroupConversationParticipantsAddedSubscriber(
    IMediator mediator,
    ILogger<GroupConversationParticipantsAddedSubscriber> logger)
    : SubscriberBase<GroupConversationParticipantsAddedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        GroupConversationParticipantsAddedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var command = new RouteGroupConversationParticipantsAddedCommand(
            message.ConversationId,
            (message.ParticipantUserIds ?? [])
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray(),
            message.ConversationMembershipRevision);

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
