using FluentAssertions;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;

public sealed class BatchAggregateRootDomainEventHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenExecuteAsyncFails_ThrowsResultExceptionWithoutRunningMutationPipeline()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var expectedError = DomainError.Conflict("Execution failed.");
        var dispatcherMock = CreateDispatcherMock();
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>>();
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [aggregate],
            FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>>.Failure(expectedError),
            [processorMock.Object],
            [deltaProcessorMock.Object]);

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<ResultException>();
        exception.Which.Result.Error.Should().Be(expectedError);
        AssertUnchanged(aggregate);
        VerifyNeverDispatched(dispatcherMock);
        VerifyNeverProcessed(processorMock);
        VerifyNeverDeltaProcessed(deltaProcessorMock);
    }

    [Fact]
    public async Task Handle_WhenMutationListIsEmpty_CompletesWithoutRunningPipeline()
    {
        var dispatcherMock = CreateDispatcherMock();
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [],
            Success([], BatchOperationType.Unspecified),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        handler.AggregateRootsCount.Should().Be(0);
        VerifyNeverDispatched(dispatcherMock);
        VerifyNeverDeltaProcessed(deltaProcessorMock);
    }

    [Fact]
    public async Task Handle_WhenMutationListIsNull_ThrowsInvalidOperationException()
    {
        var executionResult = FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>>.Success(
            new BatchAggregateDomainEventMutation<TestAggregate>(
                BatchOperationType.Unspecified,
                null!));
        var handler = CreateHandler(CreateDispatcherMock().Object, [], executionResult);

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*mutation list cannot be null*");
    }

    [Fact]
    public async Task Handle_WhenBatchContainsAllMutationTypes_AppliesAuditAndIgnoresUnchangedAndExtraRoots()
    {
        var created = new TestAggregate(Guid.NewGuid());
        var updated = new TestAggregate(Guid.NewGuid());
        var deleted = new TestAggregate(Guid.NewGuid());
        var unchanged = new TestAggregate(Guid.NewGuid());
        var extra = new TestAggregate(Guid.NewGuid());
        var dispatcherMock = CreateDispatcherMock();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [created, updated, deleted, unchanged, extra],
            Success(
                [
                    Descriptor(created, MutationType.Created),
                    Descriptor(updated, MutationType.Updated),
                    Descriptor(deleted, MutationType.Deleted),
                    Descriptor(unchanged, MutationType.Unchanged)
                ],
                BatchOperationType.Mixed));

        await handler.Handle(new TestNotification(), CancellationToken.None);

        created.Version.Should().Be(2);
        created.CreatedBy.Should().Be("system");
        created.LastModifiedBy.Should().Be("system");
        created.IsDeleted.Should().BeFalse();

        updated.Version.Should().Be(2);
        updated.CreatedBy.Should().BeEmpty();
        updated.LastModifiedBy.Should().Be("system");
        updated.IsDeleted.Should().BeFalse();

        deleted.Version.Should().Be(2);
        deleted.CreatedBy.Should().BeEmpty();
        deleted.LastModifiedBy.Should().Be("system");
        deleted.IsDeleted.Should().BeTrue();

        AssertUnchanged(unchanged);
        AssertUnchanged(extra);
        dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WhenAggregatesRaiseEvents_StampsAndDispatchesEventsInMutationOrder()
    {
        var firstAggregate = new TestAggregate(Guid.NewGuid());
        var secondAggregate = new TestAggregate(Guid.NewGuid());
        var firstEvent = new RaisedTestEvent();
        var secondEvent = new RaisedTestEvent();
        firstAggregate.RaiseEvent(firstEvent);
        secondAggregate.RaiseEvent(secondEvent);
        var dispatchedEvents = new List<ILocalEvent>();
        var dispatcherMock = new Mock<ILocalEventDispatcher>();
        dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ILocalEvent>, CancellationToken>((events, _) => dispatchedEvents.AddRange(events))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(
            dispatcherMock.Object,
            [firstAggregate, secondAggregate],
            Success(
                [
                    Descriptor(secondAggregate, MutationType.Updated),
                    Descriptor(firstAggregate, MutationType.Updated)
                ],
                BatchOperationType.Updated));

        await handler.Handle(new TestNotification(), CancellationToken.None);

        dispatchedEvents.Should().Equal(secondEvent, firstEvent);
        firstEvent.Version.Should().Be(2);
        secondEvent.Version.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenProcessorsAreRegistered_RunsDeltaProcessorsOnceAfterAllAggregateProcessors()
    {
        var firstAggregate = new TestAggregate(Guid.NewGuid());
        var secondAggregate = new TestAggregate(Guid.NewGuid());
        var unchangedAggregate = new TestAggregate(Guid.NewGuid());
        var notification = new TestNotification();
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        var callOrder = new List<string>();
        var aggregateProcessorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>>();
        aggregateProcessorMock
            .Setup(x => x.ProcessAsync(
                notification,
                It.IsAny<TestAggregate>(),
                It.IsAny<MutationType>(),
                cancellationToken))
            .Callback<TestNotification, TestAggregate, MutationType, CancellationToken>(
                (_, aggregate, _, _) => callOrder.Add($"aggregate:{aggregate.Id.Value}"))
            .Returns(Task.CompletedTask);
        var firstDeltaProcessorMock = CreateDeltaProcessorMock("delta-1", callOrder);
        var secondDeltaProcessorMock = CreateDeltaProcessorMock("delta-2", callOrder);
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [firstAggregate, secondAggregate, unchangedAggregate],
            Success(
                [
                    Descriptor(secondAggregate, MutationType.Updated),
                    Descriptor(unchangedAggregate, MutationType.Unchanged),
                    Descriptor(firstAggregate, MutationType.Deleted)
                ],
                BatchOperationType.Mixed),
            [aggregateProcessorMock.Object],
            [firstDeltaProcessorMock.Object, secondDeltaProcessorMock.Object]);

        await handler.Handle(notification, cancellationToken);

        callOrder.Should().Equal(
            $"aggregate:{secondAggregate.Id.Value}",
            $"aggregate:{firstAggregate.Id.Value}",
            "delta-1",
            "delta-2");
        firstDeltaProcessorMock.Verify(
            x => x.ProcessAsync(
                notification,
                It.Is<AggregateDeltaBatch<TestAggregate>>(batch =>
                    batch.DeltaProjectionMetadata != null &&
                    batch.Mutations.Count == 2 &&
                    ReferenceEquals(batch.Mutations[0].Aggregate, secondAggregate) &&
                    batch.Mutations[0].MutationType == MutationType.Updated &&
                    ReferenceEquals(batch.Mutations[1].Aggregate, firstAggregate) &&
                    batch.Mutations[1].MutationType == MutationType.Deleted),
                cancellationToken),
            Times.Once);
        secondDeltaProcessorMock.Verify(
            x => x.ProcessAsync(
                notification,
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                cancellationToken),
            Times.Once);
        AssertUnchanged(unchangedAggregate);
    }

    [Fact]
    public async Task Handle_WhenBatchContainsUnchangedMutation_PassesExplicitTypeAndOnlyChangedAggregate()
    {
        var updatedAggregate = new TestAggregate(Guid.NewGuid());
        var unchangedAggregate = new TestAggregate(Guid.NewGuid());
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [updatedAggregate, unchangedAggregate],
            Success(
                [
                    Descriptor(updatedAggregate, MutationType.Updated),
                    Descriptor(unchangedAggregate, MutationType.Unchanged)
                ],
                BatchOperationType.Updated),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.Is<AggregateDeltaBatch<TestAggregate>>(batch =>
                    batch.DeltaProjectionMetadata != null &&
                    batch.Mutations.Count == 1 &&
                    ReferenceEquals(batch.Mutations[0].Aggregate, updatedAggregate)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        AssertUnchanged(unchangedAggregate);
    }

    [Fact]
    public async Task Handle_WhenPipelineRuns_DispatchesBeforeAuditAndRunsDeltaAfterAggregateProcessor()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new RaisedTestEvent());
        var callOrder = new List<string>();
        var dispatcherMock = new Mock<ILocalEventDispatcher>();
        dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                aggregate.Version.Should().Be(2);
                aggregate.LastModifiedBy.Should().BeEmpty();
                callOrder.Add("dispatch");
            })
            .Returns(Task.CompletedTask);
        var aggregateProcessorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>>();
        aggregateProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                aggregate,
                MutationType.Updated,
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                aggregate.LastModifiedBy.Should().Be("system");
                callOrder.Add("aggregate-processor");
            })
            .Returns(Task.CompletedTask);
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        deltaProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("delta-processor"))
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(
            dispatcherMock.Object,
            [aggregate],
            Success(
                [Descriptor(aggregate, MutationType.Updated)],
                BatchOperationType.Updated),
            [aggregateProcessorMock.Object],
            [deltaProcessorMock.Object]);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        callOrder.Should().Equal("dispatch", "aggregate-processor", "delta-processor");
    }

    [Fact]
    public async Task Handle_WhenLaterMutationReferencesMissingAggregate_ThrowsBeforeMutatingAnyAggregate()
    {
        var availableAggregate = new TestAggregate(Guid.NewGuid());
        var missingAggregate = new TestAggregate(Guid.NewGuid());
        var raisedEvent = new RaisedTestEvent();
        availableAggregate.RaiseEvent(raisedEvent);
        var dispatcherMock = CreateDispatcherMock();
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>>();
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [availableAggregate],
            Success(
                [
                    Descriptor(availableAggregate, MutationType.Updated),
                    Descriptor(missingAggregate, MutationType.Updated)
                ],
                BatchOperationType.Updated),
            [processorMock.Object],
            [deltaProcessorMock.Object]);

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is not available*");
        AssertUnchanged(availableAggregate);
        availableAggregate.DomainEvents.Should().ContainSingle().Which.Should().BeSameAs(raisedEvent);
        VerifyNeverDispatched(dispatcherMock);
        VerifyNeverProcessed(processorMock);
        VerifyNeverDeltaProcessed(deltaProcessorMock);
    }

    [Fact]
    public async Task Handle_WhenMutationIdIsDuplicated_ThrowsBeforeMutatingAnyAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                [
                    Descriptor(aggregate, MutationType.Updated),
                    Descriptor(aggregate, MutationType.Deleted)
                ],
                BatchOperationType.Mixed));

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicate aggregate id*");
        AssertUnchanged(aggregate);
    }

    [Fact]
    public async Task Handle_WhenAggregateProcessorFails_DoesNotRunDeltaProcessors()
    {
        var firstAggregate = new TestAggregate(Guid.NewGuid());
        var secondAggregate = new TestAggregate(Guid.NewGuid());
        var expectedException = new InvalidOperationException("Aggregate processor failed.");
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                firstAggregate,
                MutationType.Updated,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [firstAggregate, secondAggregate],
            Success(
                [
                    Descriptor(firstAggregate, MutationType.Updated),
                    Descriptor(secondAggregate, MutationType.Updated)
                ],
                BatchOperationType.Updated),
            [processorMock.Object],
            [deltaProcessorMock.Object]);

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        VerifyNeverDeltaProcessed(deltaProcessorMock);
        secondAggregate.Version.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenDeltaProcessorFails_DoesNotRunRemainingDeltaProcessors()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var expectedException = new InvalidOperationException("Delta processor failed.");
        var firstDeltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        firstDeltaProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);
        var secondDeltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                [Descriptor(aggregate, MutationType.Updated)],
                BatchOperationType.Updated),
            beforeSaveDeltaProcessors: [firstDeltaProcessorMock.Object, secondDeltaProcessorMock.Object]);

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        VerifyNeverDeltaProcessed(secondDeltaProcessorMock);
    }

    [Fact]
    public async Task Handle_WhenBatchOperationTypeDiffersFromItemMutations_PassesExplicitTypeToDeltaProcessor()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                [Descriptor(aggregate, MutationType.Created)],
                BatchOperationType.Updated),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        await handler.Handle(new TestNotification(), CancellationToken.None);

        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.Is<AggregateDeltaBatch<TestAggregate>>(batch =>
                    batch.DeltaProjectionMetadata != null &&
                    batch.Mutations.Count == 1 &&
                    batch.Mutations[0].MutationType == MutationType.Created),
                It.IsAny<CancellationToken>()),
            Times.Once);
        aggregate.Version.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenBatchOperationTypeIsUnsupported_ThrowsBeforeMutatingAnyAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                [Descriptor(aggregate, MutationType.Updated)],
                (BatchOperationType)int.MaxValue));

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unsupported batch operation type*");
        AssertUnchanged(aggregate);
    }

    [Fact]
    public async Task Handle_WhenChangedMutationUsesUnspecifiedBatchOperation_ThrowsBeforeMutatingAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                [Descriptor(aggregate, MutationType.Updated)],
                BatchOperationType.Unspecified));

        var action = () => handler.Handle(new TestNotification(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unspecified batch operation type*");
        AssertUnchanged(aggregate);
    }

    private static ConfigurableBatchDomainEventHandler CreateHandler(
        ILocalEventDispatcher dispatcher,
        IEnumerable<TestAggregate> aggregateRoots,
        FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>> executionResult,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>>? beforeSaveProcessors = null,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>? beforeSaveDeltaProcessors = null)
        => new(
            dispatcher,
            aggregateRoots,
            executionResult,
            beforeSaveProcessors ?? [],
            beforeSaveDeltaProcessors ?? []);

    private static FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>> Success(
        IReadOnlyList<AggregateMutationDescriptor<TestAggregate>> mutations,
        BatchOperationType batchOperationType)
        => FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>>.Success(
            new BatchAggregateDomainEventMutation<TestAggregate>(
                batchOperationType,
                mutations,
                batchOperationType == BatchOperationType.Unspecified
                    ? null
                    : new DeltaProjectionMetadataV2(Guid.NewGuid(), 1)));

    private static AggregateMutationDescriptor<TestAggregate> Descriptor(
        TestAggregate aggregate,
        MutationType mutationType)
        => new(aggregate.Id, mutationType);

    private static Mock<ILocalEventDispatcher> CreateDispatcherMock()
    {
        var dispatcherMock = new Mock<ILocalEventDispatcher>();
        dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return dispatcherMock;
    }

    private static Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>> CreateDeltaProcessorMock(
        string name,
        ICollection<string> callOrder)
    {
        var processorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(name))
            .Returns(Task.CompletedTask);

        return processorMock;
    }

    private static void VerifyNeverDispatched(Mock<ILocalEventDispatcher> dispatcherMock)
        => dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);

    private static void VerifyNeverProcessed(
        Mock<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>> processorMock)
        => processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

    private static void VerifyNeverDeltaProcessed(
        Mock<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>> processorMock)
        => processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestNotification>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

    private static void AssertUnchanged(TestAggregate aggregate)
    {
        aggregate.Version.Should().Be(1);
        aggregate.CreatedBy.Should().BeEmpty();
        aggregate.LastModifiedBy.Should().BeEmpty();
        aggregate.IsDeleted.Should().BeFalse();
    }

    public sealed class TestNotification : DomainEventBase;

    public sealed class RaisedTestEvent : DomainEventBase;

    public sealed class TestAggregate(Guid id) : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id))
    {
        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    private sealed class ConfigurableBatchDomainEventHandler
        : BatchAggregateRootDomainEventHandlerBase<TestNotification, TestAggregate>
    {
        private readonly FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>> _executionResult;

        public ConfigurableBatchDomainEventHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<TestAggregate> aggregateRoots,
            FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>> executionResult,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>> beforeSaveProcessors,
            IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>> beforeSaveDeltaProcessors)
            : base(localEventsDispatcher, beforeSaveProcessors, beforeSaveDeltaProcessors)
        {
            AggregateRoots = aggregateRoots.ToDictionary(aggregate => aggregate.Id);
            _executionResult = executionResult;
        }

        public int AggregateRootsCount => AggregateRoots.Count;

        protected override Task<FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>>> ExecuteAsync(
            TestNotification notification,
            CancellationToken cancellationToken)
            => Task.FromResult(_executionResult);
    }
}
