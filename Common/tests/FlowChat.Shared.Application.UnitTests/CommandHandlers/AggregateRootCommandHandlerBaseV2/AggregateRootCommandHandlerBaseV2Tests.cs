using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public sealed class AggregateRootCommandHandlerBaseV2Tests
{
    [Theory]
    [InlineData(AggregateState.Created)]
    [InlineData(AggregateState.Updated)]
    [InlineData(AggregateState.Deleted)]
    public async Task Handle_WhenCommandSucceeds_PassesAggregateStateToBeforeSaveProcessor(AggregateState expectedAggregateState)
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var command = new TestCommand();
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        AggregateState? capturedAggregateState = null;
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestCommand, TestAggregate, AggregateState, CancellationToken>(
                (_, _, aggregateState, _) => capturedAggregateState = aggregateState)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(
            expectedAggregateState,
            aggregate,
            unitOfWorkMock.Object,
            localEventDispatcherMock.Object,
            [processorMock.Object]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedAggregateState.Should().Be(expectedAggregateState);
        processorMock.Verify(
            x => x.ProcessAsync(command, aggregate, expectedAggregateState, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCommandSucceeds_DispatchesLocalEventsBeforeBeforeSaveProcessors()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.AddTestDomainEvent();
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var callOrder = new List<string>();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("events"))
            .Returns(Task.CompletedTask);
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("processor"))
            .Returns(Task.CompletedTask);
        var handler = new TestUpdateCommandHandler(
            aggregate,
            unitOfWorkMock.Object,
            localEventDispatcherMock.Object,
            [processorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        callOrder.Should().Equal("events", "processor");
    }

    [Fact]
    public async Task Handle_WhenUpsertCommandCreatesAggregate_PassesCreatedOperationTypeToBeforeSaveProcessor()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var command = new TestCommand();
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        AggregateState? capturedAggregateState = null;
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestCommand, TestAggregate, AggregateState, CancellationToken>(
                (_, _, aggregateState, _) => capturedAggregateState = aggregateState)
            .Returns(Task.CompletedTask);
        var handler = new TestUpsertCommandHandler(
            aggregate,
            wasAggregateCreated: true,
            unitOfWorkMock.Object,
            localEventDispatcherMock.Object,
            [processorMock.Object]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedAggregateState.Should().Be(AggregateState.Created);
        processorMock.Verify(
            x => x.ProcessAsync(command, aggregate, AggregateState.Created, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUpsertCommandUpdatesAggregate_PassesUpdatedOperationTypeToBeforeSaveProcessor()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var command = new TestCommand();
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        AggregateState? capturedAggregateState = null;
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestCommand, TestAggregate, AggregateState, CancellationToken>(
                (_, _, aggregateState, _) => capturedAggregateState = aggregateState)
            .Returns(Task.CompletedTask);
        var handler = new TestUpsertCommandHandler(
            aggregate,
            wasAggregateCreated: false,
            unitOfWorkMock.Object,
            localEventDispatcherMock.Object,
            [processorMock.Object]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedAggregateState.Should().Be(AggregateState.Updated);
        processorMock.Verify(
            x => x.ProcessAsync(command, aggregate, AggregateState.Updated, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAggregateStateIsUnchanged_SkipsVersionEventsAndBeforeSaveProcessors()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var initialVersion = aggregate.Version;
        aggregate.AddTestDomainEvent();
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        var handler = new TestUnchangedCommandHandler(
            aggregate,
            unitOfWorkMock.Object,
            localEventDispatcherMock.Object,
            [processorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        aggregate.Version.Should().Be(initialVersion);
        aggregate.DomainEvents.Should().ContainSingle();
        localEventDispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<AggregateState>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static ICommandHandler<TestCommand, Guid> CreateHandler(
        AggregateState aggregateState,
        TestAggregate aggregate,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher localEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors)
    {
        return aggregateState switch
        {
            AggregateState.Created => new TestInsertCommandHandler(
                aggregate,
                unitOfWork,
                localEventDispatcher,
                beforeSaveProcessors),
            AggregateState.Updated => new TestUpdateCommandHandler(
                aggregate,
                unitOfWork,
                localEventDispatcher,
                beforeSaveProcessors),
            AggregateState.Deleted => new TestDeleteCommandHandler(
                aggregate,
                unitOfWork,
                localEventDispatcher,
                beforeSaveProcessors),
            _ => throw new ArgumentOutOfRangeException(nameof(aggregateState), aggregateState, null)
        };
    }

    private static Mock<IUnitOfWork> CreateUnitOfWorkMock()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        return unitOfWorkMock;
    }

    public sealed record TestCommand : ICommand<Guid>;

    public sealed class TestAggregate : AggregateRootBase<TestAggregate>
    {
        public TestAggregate(Guid id)
            : base(Id<TestAggregate>.FromGuid(id))
        {
        }

        public void AddTestDomainEvent()
        {
            AddDomainEvent(new TestDomainEvent(Id.Value));
        }
    }

    public sealed record TestDomainEvent(Guid AggregateId) : IDomainEvent
    {
        public int Version => 1;
        public string AggregateType => nameof(TestAggregate);
        public string EventType => nameof(TestDomainEvent);
        public Guid Id { get; } = Guid.NewGuid();
        public UtcDateTimeOffset OccurredOnUtc { get; } = UtcDateTimeOffset.UtcNow;
        public string? TraceInfo => null;
    }

    private sealed class TestInsertCommandHandler
        : AggregateRootInsertCommandHandlerBaseV2<TestCommand, Guid, TestAggregate>
    {
        private readonly TestAggregate _aggregate;

        public TestInsertCommandHandler(
            TestAggregate aggregate,
            IUnitOfWork unitOfWork,
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _aggregate = aggregate;
        }

        protected override Task<FlowChatResult<Guid>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => Task.FromResult(FlowChatResult<Guid>.Success(_aggregate.Id.Value));

        protected override TestAggregate GetAggregateRoot() => _aggregate;
    }

    private sealed class TestUpdateCommandHandler
        : AggregateRootUpdateCommandHandlerBaseV2<TestCommand, Guid, TestAggregate>
    {
        private readonly TestAggregate _aggregate;

        public TestUpdateCommandHandler(
            TestAggregate aggregate,
            IUnitOfWork unitOfWork,
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _aggregate = aggregate;
        }

        protected override Task<FlowChatResult<Guid>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => Task.FromResult(FlowChatResult<Guid>.Success(_aggregate.Id.Value));

        protected override TestAggregate GetAggregateRoot() => _aggregate;
    }

    private sealed class TestDeleteCommandHandler
        : AggregateRootDeleteCommandHandlerBaseV2<TestCommand, Guid, TestAggregate>
    {
        private readonly TestAggregate _aggregate;

        public TestDeleteCommandHandler(
            TestAggregate aggregate,
            IUnitOfWork unitOfWork,
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _aggregate = aggregate;
        }

        protected override Task<FlowChatResult<Guid>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => Task.FromResult(FlowChatResult<Guid>.Success(_aggregate.Id.Value));

        protected override TestAggregate GetAggregateRoot() => _aggregate;
    }

    private sealed class TestUpsertCommandHandler
        : AggregateRootUpsertCommandHandlerBaseV2<TestCommand, Guid, TestAggregate>
    {
        private readonly TestAggregate _aggregate;
        private readonly bool _wasAggregateCreated;

        public TestUpsertCommandHandler(
            TestAggregate aggregate,
            bool wasAggregateCreated,
            IUnitOfWork unitOfWork,
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _aggregate = aggregate;
            _wasAggregateCreated = wasAggregateCreated;
        }

        protected override bool WasAggregateCreated => _wasAggregateCreated;

        protected override Task<FlowChatResult<Guid>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => Task.FromResult(FlowChatResult<Guid>.Success(_aggregate.Id.Value));

        protected override TestAggregate GetAggregateRoot() => _aggregate;
    }

    private sealed class TestUnchangedCommandHandler
        : AggregateRootUpdateCommandHandlerBaseV2<TestCommand, Guid, TestAggregate>
    {
        private readonly TestAggregate _aggregate;

        public TestUnchangedCommandHandler(
            TestAggregate aggregate,
            IUnitOfWork unitOfWork,
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _aggregate = aggregate;
        }

        protected override Task<FlowChatResult<Guid>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => Task.FromResult(FlowChatResult<Guid>.Success(_aggregate.Id.Value));

        protected override TestAggregate GetAggregateRoot() => _aggregate;

        protected override AggregateState GetAggregateState(TestCommand request, TestAggregate aggregateRoot) =>
            AggregateState.Unchanged;
    }
}
