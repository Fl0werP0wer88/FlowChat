using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Core.Messaging;

namespace FlowChat.ChatService.Application.Common.Eventing.Handlers;

public abstract class MappedDomainEventHandlerBase<TDomainEvent, TIntegrationEvent>
    : DomainEventHandlerBase<TDomainEvent, TIntegrationEvent>
    where TDomainEvent : DomainEventBase
    where TIntegrationEvent : IntegrationEvent
{
    private readonly IMapper _mapper;

    protected MappedDomainEventHandlerBase(
        IIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
        : base(integrationEventPublisher)
    {
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    protected override TIntegrationEvent MapToIntegrationEvent(TDomainEvent notification) =>
        _mapper.Map<TIntegrationEvent>(notification);

    protected override Task ExecuteAsync(TDomainEvent notification, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

