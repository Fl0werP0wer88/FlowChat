using FlowChat.Core.Messaging.ChatService.Events;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class GroupConversationChangedSubscriber(
    IMediator mediator,
    ILogger<GroupConversationChangedSubscriber> logger)
    : SubscriberBase<GroupConversationChangedIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        GroupConversationChangedIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var command = new RouteGroupConversationChangedCommand(
            message.ConversationId,
            message.Type,
            message.Name,
            message.CreatedByUserId);

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
