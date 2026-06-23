using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

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
        var @event = aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>>().Subject;

        stateChangedEvents.Should().ContainSingle();
        @event.AggregateState.Should().Be(new TestSnapshot("second"));
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

        var @event = aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>>().Subject;

        calls.Should().Be(2);
        @event.AggregateState.Should().Be(new TestSnapshot("second"));
    }

    [Fact]
    public void AggregateStateChangedDomainEvent_SetsExpectedMetadataAndSnapshot()
    {
        var aggregate = TestAggregate.Create();
        var before = DateTimeOffset.UtcNow;

        aggregate.MarkChanged(() => new TestSnapshot("expected"));

        var after = DateTimeOffset.UtcNow;
        var @event = aggregate.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>>().Subject;

        @event.AggregateId.Should().Be(aggregate.Id.Value);
        @event.AggregateType.Should().Be(TestAggregate.AggregateTypeName);
        @event.EventType.Should().Be(
            DomainEventBase.GetEventType(typeof(AggregateStateChangedDomainEvent<TestAggregate, TestSnapshot>), TestAggregate.AggregateTypeName));
        @event.OccurredOnUtc.Offset.Should().Be(TimeSpan.Zero);
        @event.OccurredOnUtc.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        @event.AggregateState.Should().Be(new TestSnapshot("expected"));
    }

    [Fact]
    public void MarkAggregateStateChanged_CanCoexistWithBusinessEvents()
    {
        var aggregate = TestAggregate.Create();

        aggregate.AddBusinessEvent();
        aggregate.MarkChanged(() => new TestSnapshot("expected"));

        aggregate.DomainEvents.OfType<TestBusinessDomainEvent>().Should().ContainSingle();
        aggregate.DomainEvents.OfType<IAggregateStateChangedDomainEvent>().Should().ContainSingle();
        aggregate.DomainEvents.Should().HaveCount(2);
    }

    private sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        public const string AggregateTypeName = "tests.test-aggregate";

        private TestAggregate(Id<TestAggregate> id) : base(id)
        {
        }

        public static TestAggregate Create() => new(Id<TestAggregate>.New());

        public void MarkChanged(Func<TestSnapshot> snapshotFactory) =>
            MarkAggregateStateChanged(AggregateTypeName, snapshotFactory);

        public void AddBusinessEvent() =>
            AddDomainEvent(new TestBusinessDomainEvent(Id));
    }

    private sealed record TestSnapshot(string Value);

    [AggregateType(TestAggregate.AggregateTypeName)]
    private sealed class TestBusinessDomainEvent(Id<TestAggregate> aggregateId)
        : DomainEventBase(aggregateId, UtcDateTimeOffset.UtcNow);
}
