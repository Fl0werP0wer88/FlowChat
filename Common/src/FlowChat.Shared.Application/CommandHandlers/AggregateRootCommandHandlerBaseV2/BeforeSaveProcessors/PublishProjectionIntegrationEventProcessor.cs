using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;

public class PublishProjectionIntegrationEventProcessor<TCommand, TAggregate, TTargetReadModel>
    : IAggregateBeforeSaveProcessor<TCommand, TAggregate>
    where TAggregate : class, IAggregateRoot, IEntity<TAggregate>
    where TTargetReadModel : notnull
{
    private readonly IMapper _mapper;
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;

    public PublishProjectionIntegrationEventProcessor(
        IMapper mapper,
        IOutboxIntegrationEventPublisher integrationEventPublisher)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
    }

    public async Task ProcessAsync(
        TCommand command,
        TAggregate aggregate,
        OperationTypes operationType,
        CancellationToken cancellationToken)
    {
        var readModel = _mapper.Map<TTargetReadModel>(aggregate);
        var integrationEvent = new ProjectionIntegrationEvent<TTargetReadModel>
        {
            Value = readModel,
            Operation = operationType
        };
        var envelope = new IntegrationEventEnvelope<ProjectionIntegrationEvent<TTargetReadModel>>(
            integrationEvent,
            aggregate.Id.Value.ToString("D"));

        await _integrationEventPublisher.PublishAsync(envelope, cancellationToken);
    }
}
