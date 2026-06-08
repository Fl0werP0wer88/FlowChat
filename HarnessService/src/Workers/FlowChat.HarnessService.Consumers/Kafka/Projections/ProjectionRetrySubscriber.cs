using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.HarnessService.Consumers.Services;
using FlowChat.HarnessService.Consumers.Services.Projections.Contracts;
using Silverback.Messaging.Subscribers;

namespace FlowChat.HarnessService.Consumers.Kafka.Projections;

public sealed class ProjectionRetrySubscriber(
    IHarnessApiClient harnessApiClient,
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

        var item = new BulkUpsertProjectionRequestItem
        {
            Id = message.SourceAggregateId,
            Payload = message.Operation == OperationType.Deleted ? null : message.Value.Payload,
            SourceVersion = message.SourceAggregateVersion,
            SourceCreatedAtUtc = message.SourceAggregateCreatedAtUtc,
            SourceLastModifiedAtUtc = message.SourceAggregateModifiedAtUtc,
            SourceDeletedAtUtc = message.SourceAggregateDeletedAt
        };

        await harnessApiClient.BulkUpsertProjectionAsync(
            new BulkUpsertProjectionRequest { Items = [item] },
            cancellationToken);

        logger.LogInformation(
            "Processed projection event {Id} from Kafka retry topic.",
            item.Id);
    }
}
