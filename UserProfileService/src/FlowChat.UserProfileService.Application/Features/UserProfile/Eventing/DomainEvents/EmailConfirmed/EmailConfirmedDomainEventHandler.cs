using AutoMapper;
using FlowChat.Shared.Application;
using FlowChat.Core.Messaging.UserProfileService.Events;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.EmailConfirmed;

public sealed class EmailConfirmedDomainEventHandler
    : IDomainEventHandler<EmailConfirmedDomainEvent>
{
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher;
    private readonly IMapper _mapper;

    public EmailConfirmedDomainEventHandler(
        IOutboxIntegrationEventPublisher integrationEventPublisher,
        IMapper mapper)
    {
        _integrationEventPublisher = integrationEventPublisher
            ?? throw new ArgumentNullException(nameof(integrationEventPublisher));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public Task Handle(
        EmailConfirmedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var integrationEvent = _mapper.Map<UserEmailConfirmedIntegrationEvent>(notification);

        return _integrationEventPublisher.PublishAsync(
            integrationEvent,
            notification.UserProfileId.Value.ToString(),
            cancellationToken);
    }
}
