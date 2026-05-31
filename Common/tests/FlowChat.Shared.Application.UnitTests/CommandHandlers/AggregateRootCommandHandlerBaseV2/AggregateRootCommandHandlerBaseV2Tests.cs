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
    [InlineData(OperationTypes.Created)]
    [InlineData(OperationTypes.Updated)]
    [InlineData(OperationTypes.Deleted)]
    public async Task Handle_WhenCommandSucceeds_PassesOperationTypeToBeforeSaveProcessor(OperationTypes expectedOperationType)
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var command = new TestCommand();
        var unitOfWorkMock = CreateUnitOfWorkMock();
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        OperationTypes? capturedOperationType = null;
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<OperationTypes>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestCommand, TestAggregate, OperationTypes, CancellationToken>(
                (_, _, operationType, _) => capturedOperationType = operationType)
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(
            expectedOperationType,
            aggregate,
            unitOfWorkMock.Object,
            localEventDispatcherMock.Object,
            [processorMock.Object]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedOperationType.Should().Be(expectedOperationType);
        processorMock.Verify(
            x => x.ProcessAsync(command, aggregate, expectedOperationType, It.IsAny<CancellationToken>()),
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
                It.IsAny<OperationTypes>(),
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

    private static ICommandHandler<TestCommand, Guid> CreateHandler(
        OperationTypes operationType,
        TestAggregate aggregate,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher localEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors)
    {
        return operationType switch
        {
            OperationTypes.Created => new TestInsertCommandHandler(
                aggregate,
                unitOfWork,
                localEventDispatcher,
                beforeSaveProcessors),
            OperationTypes.Updated => new TestUpdateCommandHandler(
                aggregate,
                unitOfWork,
                localEventDispatcher,
                beforeSaveProcessors),
            OperationTypes.Deleted => new TestDeleteCommandHandler(
                aggregate,
                unitOfWork,
                localEventDispatcher,
                beforeSaveProcessors),
            _ => throw new ArgumentOutOfRangeException(nameof(operationType), operationType, null)
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

        protected override TestAggregate? GetAggregateRoot() => _aggregate;
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

        protected override TestAggregate? GetAggregateRoot() => _aggregate;
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

        protected override TestAggregate? GetAggregateRoot() => _aggregate;
    }
}
