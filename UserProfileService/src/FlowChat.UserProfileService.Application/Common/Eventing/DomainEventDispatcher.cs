using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.UserProfileService.Application.Common.Eventing;

public sealed class DomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
{
    private readonly IMediator _mediator = mediator;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> initialEvents, CancellationToken cancellationToken = default)
    {
        var eventQueue = new Queue<IDomainEvent>(initialEvents);

        while (eventQueue.Count > 0)
        {
            var currentEvent = eventQueue.Dequeue();
            await _mediator.Publish(currentEvent, cancellationToken);

            if (currentEvent is IAggregateRoot aggregateRoot)
            {
                var additionalEvents = aggregateRoot.PopDomainEvents();
                foreach (var additionalEvent in additionalEvents)
                {
                    eventQueue.Enqueue(additionalEvent);
                }
            }
        }
    }
}

