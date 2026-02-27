using MediatR;

namespace FlowChat.SocialGraphService.Domain.Events.Contracts;

public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{ }
