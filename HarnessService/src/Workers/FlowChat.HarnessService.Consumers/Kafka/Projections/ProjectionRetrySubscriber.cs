using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.Shared.Domain;
using MediatR;
using Silverback.Messaging.Subscribers;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionRetrySubscriber(
    IMediator mediator,
    ILogger<ProjectionRetrySubscriber> logger)
{
    [Subscribe]
    [ConsumerNameFilter(ConsumersServiceRegistration.ProjectionRetryConsumerName)]
    public async Task HandleAsync(
        ProjectionIntegrationEvent<ProjectionTestReadModel> message,
        CancellationToken cancellationToken)
    {
        if (message.SourceAggregateVersion <= 0)
            throw new NonTransientException("Payload does not contain valid SourceVersion.");

        var item = new ProjectionCommandItem(
            message.SourceAggregateId,
            message.Operation == OperationType.Deleted
                ? null
                : new ProjectionTestDto { Id = message.SourceAggregateId, Payload = message.Value.Payload },
            message.SourceAggregateVersion,
            message.SourceAggregateCreatedAtUtc,
            message.SourceAggregateModifiedAtUtc,
            message.SourceAggregateDeletedAt);

        var result = await mediator.Send(new BulkUpsertProjectionCommand([item]), cancellationToken);

        if (!result.IsSuccess)
        {
            // Preserve the existing contract: Silverback's MoveMessageErrorPolicy on the retry
            // endpoint routes isolable failures to the DLQ based on a thrown IsolableException.
            if (result.Error.FailureKind == FailureKind.Isolable)
                throw new IsolableException(result.Error.ErrorMessage ?? "Bulk upsert failed.");

            throw new NonTransientException(result.Error.ErrorMessage ?? "Bulk upsert failed.");
        }

        logger.LogInformation(
            "Processed projection event {Id} from Kafka retry topic.",
            item.Id);
    }
}
