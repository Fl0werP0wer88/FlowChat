using FlowChat.Domain.Abstractions;

namespace FlowChat.SocialGraphService.Application.Contracts;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
