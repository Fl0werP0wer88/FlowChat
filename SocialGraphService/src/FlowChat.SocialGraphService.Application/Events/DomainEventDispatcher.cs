using FlowChat.SocialGraphService.Application.Contracts;
using FlowChat.SocialGraphService.Domain.Common.Contracts;
using FlowChat.SocialGraphService.Domain.Events.Contracts;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> initialEvents, CancellationToken cancellationToken = default)
    {
        var eventQueue = new Queue<IDomainEvent>(initialEvents);

        while (eventQueue.Count > 0)
        {
            var currentEvent = eventQueue.Dequeue();

            // Publish the current event
            await _mediator.Publish(currentEvent, cancellationToken);

            // If the current event is associated with an aggregate, check for new events
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

