using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<ILocalEvent> domainEvents, CancellationToken cancellationToken = default);
}

