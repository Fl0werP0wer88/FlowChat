using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteDuetConversationCreated;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using MediatR;

namespace FlowChat.RealtimeService.Consumers.Kafka;

public sealed class DuetConversationProjectionSubscriber(
    IMediator mediator,
    ILogger<DuetConversationProjectionSubscriber> logger)
    : SubscriberBase<ProjectionIntegrationEvent<DuetConversationMembershipReadModel>>(logger)
{
    protected override async Task ExecuteAsync(
        ProjectionIntegrationEvent<DuetConversationMembershipReadModel> message,
        CancellationToken cancellationToken)
    {
        var command = new RouteDuetConversationCreatedCommand(
            message.Value.ConversationId,
            [message.Value.FirstUserId, message.Value.SecondUserId],
            message.Value.ConversationMembershipRevision);

        var result = await mediator.Send(command, cancellationToken);

        ThrowIfFailure(result);
    }
}
