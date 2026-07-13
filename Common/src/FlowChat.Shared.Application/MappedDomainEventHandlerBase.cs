using AutoMapper;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public abstract class MappedDomainEventHandlerBase<TDomainEvent, TIntegrationEvent>
    : DomainEventHandlerBase<TDomainEvent, TIntegrationEvent>
    where TDomainEvent : DomainEventBase
    where TIntegrationEvent : IntegrationEvent
{
    private readonly IMapper _mapper;

    protected MappedDomainEventHandlerBase(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    protected override Task<TIntegrationEvent> MapToIntegrationEvent(TDomainEvent notification, CancellationToken cancellationToken) =>
        Task.FromResult(_mapper.Map<TIntegrationEvent>(notification));

    protected override Task ExecuteAsync(TDomainEvent notification, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
