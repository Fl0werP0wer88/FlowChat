using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application;

public interface IDomainEventHandler<TDomainEvent> : INotificationHandler<TDomainEvent>
    where TDomainEvent : IDomainEvent
{ }

