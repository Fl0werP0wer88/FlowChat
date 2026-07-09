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
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            [processorMock.Object],
            fetchOverride: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Failure(expectedError)));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
        handler.ExecuteAsyncCalled.Should().BeFalse();
        processorMock.Verify(x => x.CaptureBeforeState(It.IsAny<TestAggregate>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFetchAggregateRootReturnsAggregate_CapturesSnapshotBeforeExecuteAsyncRuns()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var callOrder = new List<string>();
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.CaptureBeforeState(aggregate))
            .Callback(() => callOrder.Add("capture"));
        var handler = CreateHandler(
            [processorMock.Object],
            fetchOverride: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Success(aggregate)),
            onExecute: () => callOrder.Add("execute"));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.ExecuteAsyncCalled.Should().BeTrue();
        handler.ObservedAggregateRoot.Should().BeSameAs(aggregate);
        processorMock.Verify(x => x.CaptureBeforeState(aggregate), Times.Once);
        callOrder.Should().Equal("capture", "execute");
    }

    [Fact]
    public async Task Handle_WhenFetchAggregateRootReturnsNull_SkipsSnapshotButStillRunsExecuteAsync()
    {
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            [processorMock.Object],
            fetchOverride: (_, _) => Task.FromResult(FlowChatResult<TestAggregate?>.Success(null)));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.ExecuteAsyncCalled.Should().BeTrue();
        handler.ObservedAggregateRoot.Should().BeNull();
        processorMock.Verify(x => x.CaptureBeforeState(It.IsAny<TestAggregate>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFetchAggregateRootIsNotOverridden_BehavesLikeInsertAndRunsExecuteAsync()
    {
        var processorMock = new Mock<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>>();
        var handler = CreateHandler([processorMock.Object], fetchOverride: null);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        handler.ExecuteAsyncCalled.Should().BeTrue();
        handler.ObservedAggregateRoot.Should().BeNull();
        processorMock.Verify(x => x.CaptureBeforeState(It.IsAny<TestAggregate>()), Times.Never);
    }

    private static ConfigurableAggregateRootCommandHandler CreateHandler(
        IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> processors,
        Func<TestCommand, CancellationToken, Task<FlowChatResult<TestAggregate?>>>? fetchOverride,
        Action? onExecute = null)
    {
        var localEventDispatcherMock = new Mock<ILocalEventDispatcher>();
        localEventDispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, ct) => operation(ct));

        return new ConfigurableAggregateRootCommandHandler(
            localEventDispatcherMock.Object,
            unitOfWorkMock.Object,
            processors,
            fetchOverride,
            onExecute);
    }

    public sealed record TestCommand : ICommand<Guid>;

    public sealed class TestAggregate(Guid id) : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id));

    private sealed class ConfigurableAggregateRootCommandHandler
        : AggregateRootCommandHandlerBaseV3<TestCommand, Guid, TestAggregate>
    {
        private readonly Func<TestCommand, CancellationToken, Task<FlowChatResult<TestAggregate?>>>? _fetchOverride;
        private readonly Action? _onExecute;

        public ConfigurableAggregateRootCommandHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IUnitOfWork unitOfWork,
            IEnumerable<IAggregateBeforeSaveProcessor<TestCommand, TestAggregate>> beforeSaveProcessors,
            Func<TestCommand, CancellationToken, Task<FlowChatResult<TestAggregate?>>>? fetchOverride,
            Action? onExecute)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
        {
            _fetchOverride = fetchOverride;
            _onExecute = onExecute;
        }

        public bool ExecuteAsyncCalled { get; private set; }

        public TestAggregate? ObservedAggregateRoot { get; private set; }

        protected override Task<FlowChatResult<TestAggregate?>> FetchAggregateRootAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => _fetchOverride is not null
                ? _fetchOverride(request, cancellationToken)
                : base.FetchAggregateRootAsync(request, cancellationToken);

        protected override Task<FlowChatResult<Guid>> ExecuteAsync(TestCommand request, CancellationToken cancellationToken)
        {
            ExecuteAsyncCalled = true;
            ObservedAggregateRoot = AggregateRoot;
            _onExecute?.Invoke();

            if (AggregateRoot is null)
            {
                // Mirrors real Insert/Upsert handlers, which assign a freshly created
                // aggregate themselves when nothing was pre-fetched.
                AggregateRoot = new TestAggregate(Guid.NewGuid());
                SetMutationType(MutationType.Created);
            }

            return Task.FromResult(FlowChatResult<Guid>.Success(Guid.NewGuid()));
        }
    }
}
