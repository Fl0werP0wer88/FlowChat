using FlowChat.Shared.Application.Common.Eventing;
using FlowChat.Core.Messaging;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.Common.Eventing;

public sealed class FlowChatDomainEventDispatcherTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly LocalEventDispatcher _dispatcher;

    public FlowChatDomainEventDispatcherTests()
    {
        _dispatcher = new LocalEventDispatcher(_mediatorMock.Object);
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

        _mediatorMock.Verify(
            x => x.Publish(It.Is<ILocalEvent>(e => ReferenceEquals(e, domainEvent)), It.IsAny<CancellationToken>()),
            Times.Once);
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

        _mediatorMock.Verify(
            x => x.Publish(It.Is<ILocalEvent>(e => ReferenceEquals(e, domainEvent)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
        public int Version => 1;
        public string AggregateType => "Test";
        public string EventType => "TestEvent";
        public Guid Id { get; } = Guid.NewGuid();
        public UtcDateTimeOffset OccurredOnUtc { get; } = UtcDateTimeOffset.UtcNow;
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
        public UtcDateTimeOffset OccurredOnUtc { get; } = UtcDateTimeOffset.UtcNow;
        public Guid AggregateId { get; } = Guid.NewGuid();
        public string? TraceInfo => null;
        public string CreatedBy { get; private set; } = string.Empty;
        public UtcDateTimeOffset CreatedAtUtc { get; private set; } = UtcDateTimeOffset.UtcNow;
        public string LastModifiedBy { get; private set; } = string.Empty;
        public UtcDateTimeOffset LastModifiedAtUtc { get; private set; } = UtcDateTimeOffset.UtcNow;
        public UtcDateTimeOffset? DeletedAt { get; private set; }
        public bool IsDeleted => DeletedAt is not null;

        public IReadOnlyCollection<IDomainEvent> DomainEvents => _additionalEvents;

        public void SetCreated(string createdBy)
        {
            CreatedBy = createdBy;
            CreatedAtUtc = UtcDateTimeOffset.UtcNow;
        }

        public void SetUpdated(string lastModifiedBy)
        {
            LastModifiedBy = lastModifiedBy;
            LastModifiedAtUtc = UtcDateTimeOffset.UtcNow;
        }

        public void Delete(UtcDateTimeOffset deletedAt)
        {
            DeletedAt ??= deletedAt;
        }

        public void ClearEvents()
        {
        }

        public IReadOnlyCollection<IDomainEvent> PopDomainEvents()
        {
            return _additionalEvents;
        }

        public void IncrementVersion()
        {
        }
    }
}
