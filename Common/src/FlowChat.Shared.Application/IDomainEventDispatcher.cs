using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

