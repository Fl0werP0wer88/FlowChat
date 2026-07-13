using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Features.Account.Eventing.DomainEvents.AccountConfirmed;

public sealed class AccountConfirmedDomainEventHandler
    : IDomainEventHandler<AccountConfirmedDomainEvent>
{
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private readonly IMapper _mapper;

    public AccountConfirmedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
    {
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public Task Handle(
        AccountConfirmedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = _mapper.Map<AccountConfirmedIntegrationEvent>(notification);

        return _integrationEventPublisher.PublishAsync(
            integrationEvent,
            notification.AccountId.Value.ToString(),
            cancellationToken);
    }
}

