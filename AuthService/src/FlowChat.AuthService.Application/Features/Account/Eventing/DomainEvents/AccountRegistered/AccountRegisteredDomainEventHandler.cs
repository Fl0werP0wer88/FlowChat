using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.AuthService.Domain.Entities.Account.Events;
using FlowChat.Core.Messaging.AuthService.Events;

namespace FlowChat.AuthService.Application.Features.Account.Eventing.DomainEvents.AccountRegistered;

public sealed class AccountRegisteredDomainEventHandler
    : IDomainEventHandler<AccountRegisteredDomainEvent>
{
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private readonly IMapper _mapper;

    public AccountRegisteredDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
    {
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public Task Handle(
        AccountRegisteredDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = _mapper.Map<AccountRegisteredIntegrationEvent>(notification);

        return _integrationEventPublisher.PublishAsync(
            integrationEvent,
            notification.AccountId.Value.ToString(),
            cancellationToken);
    }
}

