using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application.Common.Eventing;

public sealed class FlowChatDomainEventDispatcher(IMediator mediator) : IDomainEventDispatcher
{
    private readonly IMediator _mediator = mediator;

    public async Task DispatchAsync(IEnumerable<IDomainEvent> initialEvents, CancellationToken cancellationToken = default)
    {
        var eventQueue = new Queue<IDomainEvent>(initialEvents);

        while (eventQueue.Count > 0)
        {
            var currentEvent = eventQueue.Dequeue();
            await _mediator.Publish(currentEvent, cancellationToken);

            // Domain event handlers may themselves raise new domain events on aggregates
            // (e.g. UserProfileCreated → EmailVerificationRequest created → events added).
            // Enqueue those so they are dispatched in the same unit of work.
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
