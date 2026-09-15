using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class ConversationProjectionV2Subscriber(
    IMediator mediator,
    ILogger<ConversationProjectionV2Subscriber> logger)
    : SubscriberBase<ProjectionIntegrationEvent<ConversationReadModelV2>>(logger)
{
    protected override async Task ExecuteAsync(
        ProjectionIntegrationEvent<ConversationReadModelV2> message,
        CancellationToken cancellationToken)
    {
        if (message.Value is null)
        {
            throw new NonTransientException("Conversation projection value cannot be null.");
        }

        if (message.Value.ConversationId != message.SourceAggregateId)
        {
            throw new NonTransientException(
                "Conversation projection must belong to the source conversation aggregate.");
        }

        if (message.Value.ConversationType is not (1 or 2))
        {
            throw new NonTransientException(
                $"Unsupported conversation type '{message.Value.ConversationType}'.");
        }

        var command = new RouteConversationProjectionV2Command(
            message.SourceAggregateId,
            message.Value.ConversationId,
            message.Value.ConversationType,
            message.Value.Name,
            message.Operation);

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
