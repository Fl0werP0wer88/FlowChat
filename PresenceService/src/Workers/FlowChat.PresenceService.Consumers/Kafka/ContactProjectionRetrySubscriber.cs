using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.SocialGraphService.ReadModels;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;
using FlowChat.Shared.Domain;
using MediatR;
using Silverback.Messaging.Subscribers;

namespace FlowChat.PresenceService.Consumers.Kafka;

public sealed class ContactProjectionRetrySubscriber(
    IMediator mediator,
    ILogger<ContactProjectionRetrySubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ContactRetryConsumerName)]
    public async Task HandleAsync(
        ProjectionIntegrationEvent<ContactReadModel> message,
        CancellationToken cancellationToken)
    {
        var item = ContactProjectionSubscriberHelper.MapProjectionEvent(message);

        var result = await mediator.Send(
            new BulkUpsertOrDeleteUserContactProjectionCommand([item]),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.FailureKind == FailureKind.Isolable)
                throw new IsolableException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");
        }

        logger.LogInformation(
            "Processed contact projection event {ObservedUserId}/{ObserverUserId} from Kafka retry topic.",
            item.Value.ObservedUserId,
            item.Value.ObserverUserId);
    }
}
