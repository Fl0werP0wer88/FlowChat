using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationChanged;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class ConversationChangedSubscriber(
    IMediator mediator,
    ILogger<ConversationChangedSubscriber> logger)
    : SubscriberBase<ConversationChangedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        ConversationChangedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var command = new RouteConversationChangedCommand(
            message.ConversationId,
            message.Type,
            message.Name,
            message.CreatedByUserId,
            (message.ParticipantUserIds ?? [])
                .Where(userId => userId != Guid.Empty)
                .Distinct()
                .ToArray());

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
