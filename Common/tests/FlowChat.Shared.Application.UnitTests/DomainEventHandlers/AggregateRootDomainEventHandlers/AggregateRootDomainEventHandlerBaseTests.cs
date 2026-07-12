using FluentAssertions;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.AggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.DomainEventHandlers.AggregateRootDomainEventHandlers;

public sealed class AggregateRootDomainEventHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenFetchAggregateRootFails_ThrowsResultExceptionWithoutRunningExecuteAsync()
    {
        var expectedError = DomainError.NotFound("Aggregate not found.");
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            [processorMock.Object],
            fetch: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Failure(expectedError)));

        var act = () => handler.Handle(new TestNotification(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ResultException>();
        exception.Which.Result.Error.Should().Be(expectedError);
        handler.ExecuteAsyncCalled.Should().BeFalse();
        processorMock.Verify(x => x.CaptureBeforeState(It.IsAny<TestAggregate>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFetchAggregateRootReturnsAggregate_CapturesSnapshotBeforeExecuteAsyncRuns()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var callOrder = new List<string>();
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>>();
        processorMock
            .Setup(x => x.CaptureBeforeState(aggregate))
            .Callback(() => callOrder.Add("capture"));
        var handler = CreateHandler(
            [processorMock.Object],
            fetch: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Success(aggregate)),
            onExecute: () => callOrder.Add("execute"));

        await handler.Handle(new TestNotification(), CancellationToken.None);

        handler.ExecuteAsyncCalled.Should().BeTrue();
        handler.ObservedAggregateRoot.Should().BeSameAs(aggregate);
        processorMock.Verify(x => x.CaptureBeforeState(aggregate), Times.Once);
        callOrder.Should().Equal("capture", "execute");
    }

    [Fact]
    public async Task Handle_WhenHandlerHasNoFetchStep_BehavesLikeInsertAndRunsExecuteAsync()
    {
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>>();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new InsertLikeDomainEventHandler(localEventDispatcherMock.Object, [processorMock.Object]);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        handler.ExecuteAsyncCalled.Should().BeTrue();
        processorMock.Verify(x => x.CaptureBeforeState(It.IsAny<TestAggregate>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenAggregateRaisesDomainEvents_StampsPostIncrementVersionOnDispatchedEvents()
    {
        var raisedEvent = new RaisedTestEvent();
        IEnumerable<ILocalEvent>? dispatchedEvents = null;
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents = events)
            .Returns(Task.CompletedTask);
        var handler = new InsertLikeDomainEventHandler(localEventDispatcherMock.Object, [], raisedEvent);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        dispatchedEvents.Should().ContainSingle().Which.Should().BeSameAs(raisedEvent);
        raisedEvent.Version.Should().Be(2);
    }

    private static ConfigurableAggregateRootDomainEventHandler CreateHandler(
        IEnumerable<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>> processors,
        Func<TestNotification, CancellationToken, Task<FlowChatResult<TestAggregate?>>> fetch,
        Action? onExecute = null)
    {
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new ConfigurableAggregateRootDomainEventHandler(
            localEventDispatcherMock.Object,
            processors,
            fetch,
            onExecute);
    }

    public sealed record TestNotification : IDomainEvent
    {
        public int Version => 1;
        public string AggregateType => nameof(TestAggregate);
        public string EventType => nameof(TestNotification);
        public Guid Id => Guid.NewGuid();
        public UtcDateTimeOffset OccurredOnUtc => UtcDateTimeOffset.UtcNow;
        public Guid AggregateId => Guid.NewGuid();
        public string? TraceInfo => null;
    }

    public sealed class TestAggregate(Guid id) : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id))
    {
        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    public sealed class RaisedTestEvent : DomainEventBase;

    private sealed class ConfigurableAggregateRootDomainEventHandler
        : FetchingAggregateRootDomainEventHandlerBase<TestNotification, TestAggregate>
    {
        private readonly Func<TestNotification, CancellationToken, Task<FlowChatResult<TestAggregate?>>> _fetch;
        private readonly Action? _onExecute;

        public ConfigurableAggregateRootDomainEventHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>> beforeSaveProcessors,
            Func<TestNotification, CancellationToken, Task<FlowChatResult<TestAggregate?>>> fetch,
            Action? onExecute)
            : base(localEventsDispatcher, beforeSaveProcessors)
        {
            _fetch = fetch;
            _onExecute = onExecute;
        }

        public bool ExecuteAsyncCalled { get; private set; }

        public TestAggregate? ObservedAggregateRoot { get; private set; }

        protected override Task<FlowChatResult<TestAggregate?>> FetchAggregateRootAsync(
            TestNotification notification,
            CancellationToken cancellationToken)
            => _fetch(notification, cancellationToken);

        protected override Task<FlowChatResult> ExecuteAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            ExecuteAsyncCalled = true;
            ObservedAggregateRoot = AggregateRoot;
            _onExecute?.Invoke();
            return Task.FromResult(FlowChatResult.Success());
        }
    }

    private sealed class InsertLikeDomainEventHandler
        : AggregateRootDomainEventHandlerBase<TestNotification, TestAggregate>
    {
        private readonly IDomainEvent? _domainEventToRaise;

        public InsertLikeDomainEventHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>> beforeSaveProcessors,
            IDomainEvent? domainEventToRaise = null)
            : base(localEventsDispatcher, beforeSaveProcessors)
        {
            _domainEventToRaise = domainEventToRaise;
        }

        public bool ExecuteAsyncCalled { get; private set; }

        protected override Task<FlowChatResult> ExecuteAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            ExecuteAsyncCalled = true;

            // Mirrors real Insert handlers, which create and assign the aggregate themselves.
            AggregateRoot = new TestAggregate(Guid.NewGuid());
            if (_domainEventToRaise is not null)
            {
                AggregateRoot.RaiseEvent(_domainEventToRaise);
            }
            SetMutationType(MutationType.Created);

            return Task.FromResult(FlowChatResult.Success());
        }
    }
}
