using AutoMapper;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Domain;
using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using MediatR;
using Silverback.Messaging.Subscribers;

namespace FlowChat.SocialGraphService.Consumers.Kafka;

public sealed class UserProfileProjectionRetrySubscriber(
    IMediator mediator,
    IMapper mapper,
    ILogger<UserProfileProjectionRetrySubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.UserProfileRetryConsumerName)]
    public async Task HandleAsync(
        ProjectionIntegrationEvent<UserProfileReadModel> message,
        CancellationToken cancellationToken)
    {
        var item = UserProfileSubscriberHelper.MapProjectionEvent(message, mapper);

        var result = await mediator.Send(
            new BulkUpsertOrDeleteUserProfileProjectionCommand([item]),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.FailureKind == FailureKind.Isolable)
                throw new IsolableException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");
        }

        logger.LogInformation(
            "Processed user profile projection event {UserProfileId} from Kafka retry topic.",
            item.EntityId.Value);
    }
}
