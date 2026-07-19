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

    [Fact]
    public async Task Handle_WhenExecuteAsyncFails_ThrowsResultExceptionAndSkipsAggregateMutationPipeline()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var expectedError = DomainError.Conflict("Execution failed.");
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>>();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        var handler = new SequencedDomainEventHandler(
            localEventDispatcherMock.Object,
            [processorMock.Object],
            aggregate,
            [],
            expectedError);

        var act = () => handler.Handle(new TestNotification(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ResultException>();
        exception.Which.Result.Error.Should().Be(expectedError);
        aggregate.Version.Should().Be(1);
        aggregate.CreatedBy.Should().BeEmpty();
        aggregate.LastModifiedBy.Should().BeEmpty();
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        localEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMutationIsUnchanged_SkipsAggregateMutationPipeline()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>>();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        var handler = new SequencedDomainEventHandler(
            localEventDispatcherMock.Object,
            [processorMock.Object],
            aggregate,
            [MutationType.Unchanged]);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        aggregate.Version.Should().Be(1);
        aggregate.CreatedBy.Should().BeEmpty();
        aggregate.LastModifiedBy.Should().BeEmpty();
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        localEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(MutationType.Created)]
    [InlineData(MutationType.Updated)]
    [InlineData(MutationType.Deleted)]
    public async Task Handle_WhenAggregateIsMutated_AppliesAuditAndPassesMutationToProcessors(
        MutationType mutationType)
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                aggregate,
                mutationType,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new SequencedDomainEventHandler(
            CreateLocalEventDispatcher(),
            [processorMock.Object],
            aggregate,
            [mutationType]);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        aggregate.Version.Should().Be(2);
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                aggregate,
                mutationType,
                It.IsAny<CancellationToken>()),
            Times.Once);

        switch (mutationType)
        {
            case MutationType.Created:
                aggregate.CreatedBy.Should().Be("system");
                aggregate.LastModifiedBy.Should().Be("system");
                aggregate.IsDeleted.Should().BeFalse();
                break;
            case MutationType.Updated:
                aggregate.CreatedBy.Should().BeEmpty();
                aggregate.LastModifiedBy.Should().Be("system");
                aggregate.IsDeleted.Should().BeFalse();
                break;
            case MutationType.Deleted:
                aggregate.CreatedBy.Should().BeEmpty();
                aggregate.LastModifiedBy.Should().Be("system");
                aggregate.IsDeleted.Should().BeTrue();
                break;
        }
    }

    [Fact]
    public async Task Handle_WhenSameInstanceReturnsUnchangedAfterUpdate_DoesNotReusePreviousMutation()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                aggregate,
                MutationType.Updated,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new SequencedDomainEventHandler(
            CreateLocalEventDispatcher(),
            [processorMock.Object],
            aggregate,
            [MutationType.Updated, MutationType.Unchanged]);

        await handler.Handle(new TestNotification(), CancellationToken.None);
        await handler.Handle(new TestNotification(), CancellationToken.None);

        aggregate.Version.Should().Be(2);
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                aggregate,
                MutationType.Updated,
                It.IsAny<CancellationToken>()),
            Times.Once);
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

    private static ILocalEventDispatcher CreateLocalEventDispatcher()
    {
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return localEventDispatcherMock.Object;
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

        protected override Task<FlowChatResult<MutationType>> ExecuteAsync(
            TestNotification notification,
            CancellationToken cancellationToken)
        {
            ExecuteAsyncCalled = true;
            ObservedAggregateRoot = AggregateRoot;
            _onExecute?.Invoke();
            return Task.FromResult(Mutation(MutationType.Created));
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

        protected override Task<FlowChatResult<MutationType>> ExecuteAsync(
            TestNotification notification,
            CancellationToken cancellationToken)
        {
            ExecuteAsyncCalled = true;

            // Mirrors real Insert handlers, which create and assign the aggregate themselves.
            AggregateRoot = new TestAggregate(Guid.NewGuid());
            if (_domainEventToRaise is not null)
            {
                AggregateRoot.RaiseEvent(_domainEventToRaise);
            }
            return Task.FromResult(Mutation(MutationType.Created));
        }
    }

    private sealed class SequencedDomainEventHandler
        : AggregateRootDomainEventHandlerBase<TestNotification, TestAggregate>
    {
        private readonly Queue<MutationType> _mutationTypes;
        private readonly IDomainError? _error;

        public SequencedDomainEventHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestNotification, TestAggregate>> beforeSaveProcessors,
            TestAggregate aggregate,
            IEnumerable<MutationType> mutationTypes,
            IDomainError? error = null)
            : base(localEventsDispatcher, beforeSaveProcessors)
        {
            AggregateRoot = aggregate;
            _mutationTypes = new Queue<MutationType>(mutationTypes);
            _error = error;
        }

        protected override Task<FlowChatResult<MutationType>> ExecuteAsync(
            TestNotification notification,
            CancellationToken cancellationToken)
        {
            var result = _error is not null
                ? Failure(_error)
                : Mutation(_mutationTypes.Dequeue());

            return Task.FromResult(result);
        }
    }
}
