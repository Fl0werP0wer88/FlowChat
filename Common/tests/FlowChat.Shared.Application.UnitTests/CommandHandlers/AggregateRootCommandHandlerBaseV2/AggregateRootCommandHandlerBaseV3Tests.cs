using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public sealed class AggregateRootCommandHandlerBaseV3Tests
{
    [Fact]
    public async Task Handle_WhenFetchAggregateRootFails_ReturnsFailureWithoutRunningExecuteAsync()
    {
        var expectedError = DomainError.NotFound("Aggregate not found.");
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateFetchingHandler(
            [processorMock.Object],
            fetch: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Failure(expectedError)));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
        handler.ExecuteAsyncCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenFetchAggregateRootReturnsAggregate_SetsAggregateBeforeExecuteAsyncRuns()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateFetchingHandler(
            [processorMock.Object],
            fetch: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Success(aggregate)));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.ExecuteAsyncCalled.Should().BeTrue();
        handler.ObservedAggregateRoot.Should().BeSameAs(aggregate);
    }

    [Fact]
    public async Task Handle_WhenFetchAggregateRootReturnsNull_StillRunsExecuteAsync()
    {
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateFetchingHandler(
            [processorMock.Object],
            fetch: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Success(null)));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.ExecuteAsyncCalled.Should().BeTrue();
        handler.ObservedAggregateRoot.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenHandlerHasNoFetchStep_BehavesLikeInsertAndRunsExecuteAsync()
    {
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateInsertLikeHandler([processorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.ExecuteAsyncCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAggregateRaisesDomainEvents_StampsPostIncrementVersionOnDispatchedEvents()
    {
        var domainEvent = new TestDomainEvent();
        IEnumerable<ILocalEvent>? dispatchedEvents = null;
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents = events)
            .Returns(Task.CompletedTask);
        var handler = new InsertLikeCommandHandler(localEventDispatcherMock.Object, CreateUnitOfWork(), [], domainEvent);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dispatchedEvents.Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
        domainEvent.Version.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenExecuteAsyncFails_SkipsAggregateMutationPipeline()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var expectedError = DomainError.Conflict("Execution failed.");
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        var handler = new SequencedCommandHandler(
            localEventDispatcherMock.Object,
            CreateUnitOfWork(),
            [processorMock.Object],
            aggregate,
            [],
            Guid.NewGuid(),
            expectedError);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
        aggregate.Version.Should().Be(1);
        aggregate.CreatedBy.Should().BeEmpty();
        aggregate.LastModifiedBy.Should().BeEmpty();
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        localEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMutationIsUnchanged_ReturnsResponseAndSkipsAggregateMutationPipeline()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var expectedResponse = Guid.NewGuid();
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        var handler = new SequencedCommandHandler(
            localEventDispatcherMock.Object,
            CreateUnitOfWork(),
            [processorMock.Object],
            aggregate,
            [MutationType.Unchanged],
            expectedResponse);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
        aggregate.Version.Should().Be(1);
        aggregate.CreatedBy.Should().BeEmpty();
        aggregate.LastModifiedBy.Should().BeEmpty();
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
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
        var expectedResponse = Guid.NewGuid();
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                aggregate,
                mutationType,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new SequencedCommandHandler(
            CreateLocalEventDispatcher(),
            CreateUnitOfWork(),
            [processorMock.Object],
            aggregate,
            [mutationType],
            expectedResponse);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
        aggregate.Version.Should().Be(2);
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
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
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                aggregate,
                MutationType.Updated,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new SequencedCommandHandler(
            CreateLocalEventDispatcher(),
            CreateUnitOfWork(),
            [processorMock.Object],
            aggregate,
            [MutationType.Updated, MutationType.Unchanged],
            Guid.NewGuid());

        var firstResult = await handler.Handle(new TestCommand(), CancellationToken.None);
        var secondResult = await handler.Handle(new TestCommand(), CancellationToken.None);

        firstResult.IsSuccess.Should().BeTrue();
        secondResult.IsSuccess.Should().BeTrue();
        aggregate.Version.Should().Be(2);
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                aggregate,
                MutationType.Updated,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static FetchingConfigurableCommandHandler CreateFetchingHandler(
        IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> processors,
        Func<TestCommand, CancellationToken, Task<FlowChatResult<TestAggregate?>>> fetch,
        Action? onExecute = null)
    {
        return new FetchingConfigurableCommandHandler(
            CreateLocalEventDispatcher(),
            CreateUnitOfWork(),
            processors,
            fetch,
            onExecute);
    }

    private static InsertLikeCommandHandler CreateInsertLikeHandler(
        IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> processors)
    {
        return new InsertLikeCommandHandler(
            CreateLocalEventDispatcher(),
            CreateUnitOfWork(),
            processors);
    }

    private static ILocalEventDispatcher CreateLocalEventDispatcher()
    {
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return localEventDispatcherMock.Object;
    }

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        return unitOfWorkMock.Object;
    }

    public sealed record TestCommand : ICommand<Guid>;

    public sealed class TestAggregate(Guid id) : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id))
    {
        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    public sealed class TestDomainEvent : DomainEventBase;

    private sealed class FetchingConfigurableCommandHandler
        : FetchingAggregateRootCommandHandlerBaseV3<TestCommand, Guid, TestAggregate>
    {
        private readonly Func<TestCommand, CancellationToken, Task<FlowChatResult<TestAggregate?>>> _fetch;
        private readonly Action? _onExecute;

        public FetchingConfigurableCommandHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IUnitOfWork unitOfWork,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> beforeSaveProcessors,
            Func<TestCommand, CancellationToken, Task<FlowChatResult<TestAggregate?>>> fetch,
            Action? onExecute)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _fetch = fetch;
            _onExecute = onExecute;
        }

        public bool ExecuteAsyncCalled { get; private set; }

        public TestAggregate? ObservedAggregateRoot { get; private set; }

        protected override Task<FlowChatResult<TestAggregate?>> FetchAggregateRootAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => _fetch(request, cancellationToken);

        protected override Task<FlowChatResult<AggregateMutation<Guid>>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
        {
            ExecuteAsyncCalled = true;
            ObservedAggregateRoot = AggregateRoot;
            _onExecute?.Invoke();

            if (AggregateRoot is null)
            {
                // Mirrors real Upsert handlers, which assign a freshly created
                // aggregate themselves when nothing was pre-fetched.
                AggregateRoot = new TestAggregate(Guid.NewGuid());
            }

            return Task.FromResult(Mutation(MutationType.Created, Guid.NewGuid()));
        }
    }

    private sealed class InsertLikeCommandHandler
        : AggregateRootCommandHandlerBaseV3<TestCommand, Guid, TestAggregate>
    {
        private readonly IDomainEvent? _domainEventToRaise;

        public InsertLikeCommandHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IUnitOfWork unitOfWork,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> beforeSaveProcessors,
            IDomainEvent? domainEventToRaise = null)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _domainEventToRaise = domainEventToRaise;
        }

        public bool ExecuteAsyncCalled { get; private set; }

        protected override Task<FlowChatResult<AggregateMutation<Guid>>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
        {
            ExecuteAsyncCalled = true;

            // Mirrors real Insert handlers, which create and assign the aggregate themselves.
            AggregateRoot = new TestAggregate(Guid.NewGuid());
            if (_domainEventToRaise is not null)
            {
                AggregateRoot.RaiseEvent(_domainEventToRaise);
            }
            return Task.FromResult(Mutation(MutationType.Created, Guid.NewGuid()));
        }
    }

    private sealed class SequencedCommandHandler
        : AggregateRootCommandHandlerBaseV3<TestCommand, Guid, TestAggregate>
    {
        private readonly Queue<MutationType> _mutationTypes;
        private readonly Guid _response;
        private readonly IDomainError? _error;

        public SequencedCommandHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IUnitOfWork unitOfWork,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> beforeSaveProcessors,
            TestAggregate aggregate,
            IEnumerable<MutationType> mutationTypes,
            Guid response,
            IDomainError? error = null)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            AggregateRoot = aggregate;
            _mutationTypes = new Queue<MutationType>(mutationTypes);
            _response = response;
            _error = error;
        }

        protected override Task<FlowChatResult<AggregateMutation<Guid>>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
        {
            var result = _error is not null
                ? Failure(_error)
                : Mutation(_mutationTypes.Dequeue(), _response);

            return Task.FromResult(result);
        }
    }
}
