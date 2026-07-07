using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationParticipantsRemoved;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class GroupConversationParticipantsRemovedSubscriber(
    IMediator mediator,
    ILogger<GroupConversationParticipantsRemovedSubscriber> logger)
    : SubscriberBase<GroupConversationParticipantsRemovedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        GroupConversationParticipantsRemovedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var command = new RouteGroupConversationParticipantsRemovedCommand(
            message.ConversationId,
            (message.ParticipantUserIds ?? [])
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray());

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
