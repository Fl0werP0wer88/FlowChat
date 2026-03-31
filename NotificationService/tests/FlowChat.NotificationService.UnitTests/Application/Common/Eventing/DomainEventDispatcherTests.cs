using FlowChat.NotificationService.Application.Common.Eventing;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.NotificationService.UnitTests.Application.Common.Eventing;

public sealed class DomainEventDispatcherTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly DomainEventDispatcher _dispatcher;

    public DomainEventDispatcherTests()
    {
        _dispatcher = new DomainEventDispatcher(_mediatorMock.Object);
    }

    [Fact]
    public async Task DispatchAsync_WithEmptyEvents_DoesNotPublishAnything()
    {
        await _dispatcher.DispatchAsync([], CancellationToken.None);

        _mediatorMock.Verify(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_WithSingleEvent_PublishesOnce()
    {
        var domainEvent = new TestDomainEvent();

        await _dispatcher.DispatchAsync([domainEvent], CancellationToken.None);

        _mediatorMock.Verify(x => x.Publish(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleEvents_PublishesEachInOrder()
    {
        var event1 = new TestDomainEvent();
        var event2 = new TestDomainEvent();
        var publishedEvents = new List<IDomainEvent>();

        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((e, _) => publishedEvents.Add((IDomainEvent)e))
            .Returns(Task.CompletedTask);

        await _dispatcher.DispatchAsync([event1, event2], CancellationToken.None);

        publishedEvents.Should().HaveCount(2);
        publishedEvents[0].Should().BeSameAs(event1);
        publishedEvents[1].Should().BeSameAs(event2);
    }

    [Fact]
    public async Task DispatchAsync_WhenEventIsAggregateRoot_ProcessesAdditionalEventsFromIt()
    {
        var cascadedEvent = new TestDomainEvent();
        var aggregateEvent = new TestAggregateRootDomainEvent([cascadedEvent]);

        var publishedEvents = new List<IDomainEvent>();
        _mediatorMock
            .Setup(x => x.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()))
            .Callback<INotification, CancellationToken>((e, _) => publishedEvents.Add((IDomainEvent)e))
            .Returns(Task.CompletedTask);

        await _dispatcher.DispatchAsync([aggregateEvent], CancellationToken.None);

        publishedEvents.Should().HaveCount(2);
        publishedEvents[0].Should().BeSameAs(aggregateEvent);
        publishedEvents[1].Should().BeSameAs(cascadedEvent);
    }

    [Fact]
    public async Task DispatchAsync_WhenNonAggregateRootEvent_DoesNotAttemptToCascade()
    {
        var domainEvent = new TestDomainEvent();

        await _dispatcher.DispatchAsync([domainEvent], CancellationToken.None);

        _mediatorMock.Verify(x => x.Publish(domainEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    // --- Test helpers ---

    private sealed class TestDomainEvent : IDomainEvent
    {
        public int Version => 1;
        public string AggregateType => "Test";
        public string EventType => "TestEvent";
        public Guid Id { get; } = Guid.NewGuid();
        public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
        public Guid AggregateId { get; } = Guid.NewGuid();
        public string? TraceInfo => null;
    }

    private sealed class TestAggregateRootDomainEvent(IReadOnlyCollection<IDomainEvent> additionalEvents)
        : IDomainEvent, IAggregateRoot
    {
        private readonly IReadOnlyCollection<IDomainEvent> _additionalEvents = additionalEvents;

        public int Version => 1;
        public string AggregateType => "TestAggregate";
        public string EventType => "TestAggregateEvent";
        public Guid Id { get; } = Guid.NewGuid();
        public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
        public Guid AggregateId { get; } = Guid.NewGuid();
        public string? TraceInfo => null;

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _additionalEvents;

        public void ClearEvents() { }

        public IReadOnlyCollection<IDomainEvent> PopDomainEvents()
        {
            return _additionalEvents;
        }
    }
}
