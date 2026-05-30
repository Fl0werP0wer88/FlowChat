using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public interface ILocalEventDispatcher
{
    Task DispatchAsync(IEnumerable<ILocalEvent> domainEvents, CancellationToken cancellationToken = default);
}

