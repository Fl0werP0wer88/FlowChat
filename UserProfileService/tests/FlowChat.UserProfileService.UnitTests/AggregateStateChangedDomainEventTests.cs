using FlowChat.Domain.Abstractions;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class AggregateStateChangedDomainEventTests
{
    [Fact]
    public void MarkAggregateStateChanged_WhenCalledTwice_AddsSingleAggregateStateChangedEvent()
    {
        var aggregate = TestAggregate.Create();

        aggregate.MarkChanged(() => new TestSnapshot("first"));
        aggregate.MarkChanged(() => new TestSnapshot("second"));

        var stateChangedEvents = aggregate.DomainEvents.OfType<IAggregateStateChangedDomainEvent>().ToList();
        var @event = Assert.IsType<AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>>(Assert.Single(aggregate.DomainEvents));

        Assert.Single(stateChangedEvents);
        Assert.Equal(new TestSnapshot("second"), @event.AggregateState);
    }

    [Fact]
    public void MarkAggregateStateChanged_WhenEventAlreadyExists_ReplacesItWithLatestSnapshot()
    {
        var aggregate = TestAggregate.Create();
        var calls = 0;

        aggregate.MarkChanged(() =>
        {
            calls++;
            return new TestSnapshot("first");
        });

        aggregate.MarkChanged(() =>
        {
            calls++;
            return new TestSnapshot("second");
        });

        var @event = Assert.IsType<AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>>(Assert.Single(aggregate.DomainEvents));

        Assert.Equal(2, calls);
        Assert.Equal(new TestSnapshot("second"), @event.AggregateState);
    }

    [Fact]
    public void AggregateStateChangedDomainEvent_SetsExpectedMetadataAndSnapshot()
    {
        var aggregate = TestAggregate.Create();
        var before = DateTimeOffset.UtcNow;

        aggregate.MarkChanged(() => new TestSnapshot("expected"));

        var after = DateTimeOffset.UtcNow;
        var @event = Assert.IsType<AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>>(Assert.Single(aggregate.DomainEvents));

        Assert.Equal(aggregate.Id.Value, @event.AggregateId);
        Assert.Equal(TestAggregate.AggregateTypeName, @event.AggregateType);
        Assert.Equal(
            DomainEventBase.GetEventType(typeof(AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>), TestAggregate.AggregateTypeName),
            @event.EventType);
        Assert.Equal(TimeSpan.Zero, @event.OccurredOnUtc.Offset);
        Assert.InRange(@event.OccurredOnUtc, before, after);
        Assert.Equal(new TestSnapshot("expected"), @event.AggregateState);
    }

    [Fact]
    public void MarkAggregateStateChanged_CanCoexistWithBusinessEvents()
    {
        var aggregate = TestAggregate.Create();

        aggregate.AddBusinessEvent();
        aggregate.MarkChanged(() => new TestSnapshot("expected"));

        Assert.Single(aggregate.DomainEvents.OfType<TestBusinessDomainEvent>());
        Assert.Single(aggregate.DomainEvents.OfType<IAggregateStateChangedDomainEvent>());
        Assert.Equal(2, aggregate.DomainEvents.Count);
    }

    private sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        public const string AggregateTypeName = "tests.test-aggregate";

        private TestAggregate(Id<TestAggregate>? id = null) : base(id)
        {
        }

        public static TestAggregate Create() => new();

        public void MarkChanged(Func<TestSnapshot> snapshotFactory) =>
            MarkAggregateStateChanged(AggregateTypeName, snapshotFactory);

        public void AddBusinessEvent() =>
            AddDomainEvent(new TestBusinessDomainEvent(Id));
    }

    private sealed record TestSnapshot(string Value);

    [AggregateType(TestAggregate.AggregateTypeName)]
    private sealed class TestBusinessDomainEvent(Id<TestAggregate> aggregateId)
        : DomainEventBase(aggregateId, DateTimeOffset.UtcNow);
}
