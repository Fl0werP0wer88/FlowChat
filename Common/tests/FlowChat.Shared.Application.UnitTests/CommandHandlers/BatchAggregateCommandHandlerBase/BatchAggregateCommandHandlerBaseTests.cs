using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using Moq;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.BatchAggregateCommandHandlerBase;

public sealed class BatchAggregateCommandHandlerBaseTests
{
    [Fact]
    public async Task Handle_WhenExecuteAsyncFails_ReturnsFailureWithoutMutatingAggregates()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var expectedError = DomainError.Conflict("Execution failed.");
        var dispatcherMock = CreateDispatcherMock();
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [aggregate],
            FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>>.Failure(expectedError),
            [processorMock.Object],
            [deltaProcessorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(expectedError);
        aggregate.Version.Should().Be(1);
        aggregate.CreatedBy.Should().BeEmpty();
        aggregate.LastModifiedBy.Should().BeEmpty();
        dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMutationListIsEmpty_ReturnsResponseWithoutDispatchingEvents()
    {
        var expectedResponse = Guid.NewGuid();
        var dispatcherMock = CreateDispatcherMock();
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [],
            Success(expectedResponse, [], BatchOperationType.Unspecified),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
        handler.AggregateRootsCount.Should().Be(0);
        dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBatchContainsAllMutationTypes_AppliesEachPipelineAndIgnoresExtraAggregate()
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
                Guid.NewGuid(),
                [
                    Descriptor(created, MutationType.Created),
                    Descriptor(updated, MutationType.Updated),
                    Descriptor(deleted, MutationType.Deleted),
                    Descriptor(unchanged, MutationType.Unchanged)
                ],
                BatchOperationType.Mixed));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

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
    public async Task Handle_WhenAggregatesRaiseEvents_StampsVersionsAndDispatchesInMutationOrder()
    {
        var firstAggregate = new TestAggregate(Guid.NewGuid());
        var secondAggregate = new TestAggregate(Guid.NewGuid());
        var firstEvent = new TestDomainEvent();
        var secondEvent = new TestDomainEvent();
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
                Guid.NewGuid(),
                [
                    Descriptor(secondAggregate, MutationType.Updated),
                    Descriptor(firstAggregate, MutationType.Updated)
                ],
                BatchOperationType.Updated));

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        dispatchedEvents.Should().Equal(secondEvent, firstEvent);
        firstEvent.Version.Should().Be(2);
        secondEvent.Version.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenProcessorsAreRegistered_RunsEachProcessorForEachChangedAggregateInMutationOrder()
    {
        var firstAggregate = new TestAggregate(Guid.NewGuid());
        var secondAggregate = new TestAggregate(Guid.NewGuid());
        var unchangedAggregate = new TestAggregate(Guid.NewGuid());
        var callOrder = new List<string>();
        var firstProcessorMock = CreateProcessorMock("processor-1", callOrder);
        var secondProcessorMock = CreateProcessorMock("processor-2", callOrder);
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [firstAggregate, secondAggregate, unchangedAggregate],
            Success(
                Guid.NewGuid(),
                [
                    Descriptor(secondAggregate, MutationType.Updated),
                    Descriptor(unchangedAggregate, MutationType.Unchanged),
                    Descriptor(firstAggregate, MutationType.Deleted)
                ],
                BatchOperationType.Mixed),
            [firstProcessorMock.Object, secondProcessorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        callOrder.Should().Equal(
            $"processor-1:{secondAggregate.Id.Value}",
            $"processor-2:{secondAggregate.Id.Value}",
            $"processor-1:{firstAggregate.Id.Value}",
            $"processor-2:{firstAggregate.Id.Value}");
        firstProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                unchangedAggregate,
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        secondProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                unchangedAggregate,
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProcessorRuns_ItObservesDispatchedEventsIncrementedVersionAndAppliedAudit()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.RaiseEvent(new TestDomainEvent());
        var eventsDispatched = false;
        var dispatcherMock = new Mock<ILocalEventDispatcher>();
        dispatcherMock
            .Setup(x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()))
            .Callback(() => eventsDispatched = true)
            .Returns(Task.CompletedTask);
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                aggregate,
                MutationType.Created,
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                eventsDispatched.Should().BeTrue();
                aggregate.Version.Should().Be(2);
                aggregate.CreatedBy.Should().Be("system");
                aggregate.LastModifiedBy.Should().Be("system");
            })
            .Returns(Task.CompletedTask);
        var handler = CreateHandler(
            dispatcherMock.Object,
            [aggregate],
            Success(
                Guid.NewGuid(),
                [Descriptor(aggregate, MutationType.Created)],
                BatchOperationType.Created),
            [processorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        processorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                aggregate,
                MutationType.Created,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDeltaProcessorsAreRegistered_RunsEachOnceAfterAllAggregateProcessors()
    {
        var firstAggregate = new TestAggregate(Guid.NewGuid());
        var secondAggregate = new TestAggregate(Guid.NewGuid());
        var unchangedAggregate = new TestAggregate(Guid.NewGuid());
        var callOrder = new List<string>();
        var aggregateProcessorMock = CreateProcessorMock("aggregate", callOrder);
        var firstDeltaProcessorMock = CreateDeltaProcessorMock("delta-1", callOrder);
        var secondDeltaProcessorMock = CreateDeltaProcessorMock("delta-2", callOrder);
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [firstAggregate, secondAggregate, unchangedAggregate],
            Success(
                Guid.NewGuid(),
                [
                    Descriptor(secondAggregate, MutationType.Updated),
                    Descriptor(unchangedAggregate, MutationType.Unchanged),
                    Descriptor(firstAggregate, MutationType.Deleted)
                ],
                BatchOperationType.Mixed),
            [aggregateProcessorMock.Object],
            [firstDeltaProcessorMock.Object, secondDeltaProcessorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        callOrder.Should().Equal(
            $"aggregate:{secondAggregate.Id.Value}",
            $"aggregate:{firstAggregate.Id.Value}",
            "delta-1",
            "delta-2");
        firstDeltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.Is<AggregateDeltaBatch<TestAggregate>>(batch =>
                    batch.DeltaProjectionMetadata != null &&
                    batch.Mutations.Count == 2 &&
                    ReferenceEquals(batch.Mutations[0].Aggregate, secondAggregate) &&
                    batch.Mutations[0].MutationType == MutationType.Updated &&
                    ReferenceEquals(batch.Mutations[1].Aggregate, firstAggregate) &&
                    batch.Mutations[1].MutationType == MutationType.Deleted),
                It.IsAny<CancellationToken>()),
            Times.Once);
        secondDeltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        secondAggregate.Version.Should().Be(2);
        secondAggregate.LastModifiedBy.Should().Be("system");
        firstAggregate.Version.Should().Be(2);
        firstAggregate.IsDeleted.Should().BeTrue();
        AssertUnchanged(unchangedAggregate);
    }

    [Fact]
    public async Task Handle_WhenAllMutationsAreUnchanged_DoesNotRunDeltaProcessors()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                Guid.NewGuid(),
                [Descriptor(aggregate, MutationType.Unchanged)],
                BatchOperationType.Unspecified),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBatchContainsUnchangedMutation_PassesExplicitTypeAndOnlyChangedAggregate()
    {
        var updatedAggregate = new TestAggregate(Guid.NewGuid());
        var unchangedAggregate = new TestAggregate(Guid.NewGuid());
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [updatedAggregate, unchangedAggregate],
            Success(
                Guid.NewGuid(),
                [
                    Descriptor(updatedAggregate, MutationType.Updated),
                    Descriptor(unchangedAggregate, MutationType.Unchanged)
                ],
                BatchOperationType.Updated),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.Is<AggregateDeltaBatch<TestAggregate>>(batch =>
                    batch.DeltaProjectionMetadata != null &&
                    batch.Mutations.Count == 1 &&
                    ReferenceEquals(batch.Mutations[0].Aggregate, updatedAggregate)),
                It.IsAny<CancellationToken>()),
            Times.Once);
        AssertUnchanged(unchangedAggregate);
    }

    [Fact]
    public async Task Handle_WhenAggregateProcessorFails_DoesNotRunDeltaProcessors()
    {
        var firstAggregate = new TestAggregate(Guid.NewGuid());
        var secondAggregate = new TestAggregate(Guid.NewGuid());
        var expectedException = new InvalidOperationException("Aggregate processor failed.");
        var aggregateProcessorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        aggregateProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                firstAggregate,
                MutationType.Updated,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [firstAggregate, secondAggregate],
            Success(
                Guid.NewGuid(),
                [
                    Descriptor(firstAggregate, MutationType.Updated),
                    Descriptor(secondAggregate, MutationType.Updated)
                ],
                BatchOperationType.Updated),
            [aggregateProcessorMock.Object],
            [deltaProcessorMock.Object]);

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        secondAggregate.Version.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenDeltaProcessorFails_DoesNotRunRemainingDeltaProcessors()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var expectedException = new InvalidOperationException("Delta processor failed.");
        var firstDeltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        firstDeltaProcessorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);
        var secondDeltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                Guid.NewGuid(),
                [Descriptor(aggregate, MutationType.Updated)],
                BatchOperationType.Updated),
            beforeSaveDeltaProcessors: [firstDeltaProcessorMock.Object, secondDeltaProcessorMock.Object]);

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        var exception = await action.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Should().BeSameAs(expectedException);
        secondDeltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLaterMutationReferencesMissingAggregate_ThrowsBeforeMutatingAnyAggregate()
    {
        var availableAggregate = new TestAggregate(Guid.NewGuid());
        var missingAggregate = new TestAggregate(Guid.NewGuid());
        var domainEvent = new TestDomainEvent();
        availableAggregate.RaiseEvent(domainEvent);
        var dispatcherMock = CreateDispatcherMock();
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [availableAggregate],
            Success(
                Guid.NewGuid(),
                [
                    Descriptor(availableAggregate, MutationType.Updated),
                    Descriptor(missingAggregate, MutationType.Updated)
                ],
                BatchOperationType.Updated),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is not available*");
        AssertUnchanged(availableAggregate);
        availableAggregate.DomainEvents.Should().ContainSingle().Which.Should().BeSameAs(domainEvent);
        dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMutationIdIsDuplicated_ThrowsBeforeMutatingAnyAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var dispatcherMock = CreateDispatcherMock();
        var handler = CreateHandler(
            dispatcherMock.Object,
            [aggregate],
            Success(
                Guid.NewGuid(),
                [
                    Descriptor(aggregate, MutationType.Updated),
                    Descriptor(aggregate, MutationType.Deleted)
                ],
                BatchOperationType.Mixed));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*duplicate aggregate id*");
        AssertUnchanged(aggregate);
        dispatcherMock.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<ILocalEvent>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnchangedMutationReferencesMissingAggregate_ThrowsInvalidOperationException()
    {
        var missingAggregate = new TestAggregate(Guid.NewGuid());
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [],
            Success(
                Guid.NewGuid(),
                [Descriptor(missingAggregate, MutationType.Unchanged)],
                BatchOperationType.Unspecified));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*is not available*");
    }

    [Fact]
    public async Task Handle_WhenMutationListContainsNullEntry_ThrowsBeforeMutatingAnyAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        IReadOnlyList<AggregateMutationDescriptor<TestAggregate>> mutations =
            [Descriptor(aggregate, MutationType.Updated), null!];
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(Guid.NewGuid(), mutations, BatchOperationType.Updated));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cannot contain null entries*");
        AssertUnchanged(aggregate);
    }

    [Fact]
    public async Task Handle_WhenMutationListIsNull_ThrowsInvalidOperationException()
    {
        var executionResult = FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>>.Success(
            new BatchAggregateMutation<Guid, TestAggregate>(
                Guid.NewGuid(),
                BatchOperationType.Unspecified,
                null!));
        var handler = CreateHandler(CreateDispatcherMock().Object, [], executionResult);

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*mutation list cannot be null*");
    }

    [Fact]
    public async Task Handle_WhenMutationIdIsNull_ThrowsBeforeMutatingAnyAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var mutation = new AggregateMutationDescriptor<TestAggregate>(null!, MutationType.Updated);
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(Guid.NewGuid(), [mutation], BatchOperationType.Updated));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*aggregate id cannot be null*");
        AssertUnchanged(aggregate);
    }

    [Fact]
    public async Task Handle_WhenMutationTypeIsUnsupported_ThrowsBeforeMutatingAnyAggregate()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                Guid.NewGuid(),
                [Descriptor(aggregate, (MutationType)int.MaxValue)],
                BatchOperationType.Updated));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Unsupported mutation type*");
        AssertUnchanged(aggregate);
    }

    [Fact]
    public async Task Handle_WhenBatchOperationTypeDiffersFromItemMutations_PassesExplicitTypeToDeltaProcessor()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var deltaProcessorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        var handler = CreateHandler(
            CreateDispatcherMock().Object,
            [aggregate],
            Success(
                Guid.NewGuid(),
                [Descriptor(aggregate, MutationType.Created)],
                BatchOperationType.Updated),
            beforeSaveDeltaProcessors: [deltaProcessorMock.Object]);

        var result = await handler.Handle(new TestCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        deltaProcessorMock.Verify(
            x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
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
                Guid.NewGuid(),
                [Descriptor(aggregate, MutationType.Updated)],
                (BatchOperationType)int.MaxValue));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

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
                Guid.NewGuid(),
                [Descriptor(aggregate, MutationType.Updated)],
                BatchOperationType.Unspecified));

        var action = async () => await handler.Handle(new TestCommand(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unspecified batch operation type*");
        AssertUnchanged(aggregate);
    }

    private static ConfigurableBatchCommandHandler CreateHandler(
        ILocalEventDispatcher dispatcher,
        IEnumerable<TestAggregate> aggregateRoots,
        FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>> executionResult,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>? beforeSaveProcessors = null,
        IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>? beforeSaveDeltaProcessors = null)
        => new(
            dispatcher,
            CreateUnitOfWork(),
            aggregateRoots,
            executionResult,
            beforeSaveProcessors ?? [],
            beforeSaveDeltaProcessors ?? []);

    private static FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>> Success(
        Guid response,
        IReadOnlyList<AggregateMutationDescriptor<TestAggregate>> mutations,
        BatchOperationType batchOperationType)
        => FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>>.Success(
            new BatchAggregateMutation<Guid, TestAggregate>(
                response,
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

    private static Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> CreateProcessorMock(
        string name,
        ICollection<string> callOrder)
    {
        var processorMock = new Mock<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<TestAggregate>(),
                It.IsAny<MutationType>(),
                It.IsAny<CancellationToken>()))
            .Callback<TestCommand, TestAggregate, MutationType, CancellationToken>(
                (_, aggregate, _, _) => callOrder.Add($"{name}:{aggregate.Id.Value}"))
            .Returns(Task.CompletedTask);

        return processorMock;
    }

    private static Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>> CreateDeltaProcessorMock(
        string name,
        ICollection<string> callOrder)
    {
        var processorMock = new Mock<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>>();
        processorMock
            .Setup(x => x.ProcessAsync(
                It.IsAny<TestCommand>(),
                It.IsAny<AggregateDeltaBatch<TestAggregate>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add(name))
            .Returns(Task.CompletedTask);

        return processorMock;
    }

    private static IUnitOfWork CreateUnitOfWork()
    {
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        unitOfWorkMock
            .Setup(x => x.ExecuteCommandInTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<FlowChatResult<Guid>>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<FlowChatResult<Guid>>>, CancellationToken>(
                (operation, cancellationToken) => operation(cancellationToken));

        return unitOfWorkMock.Object;
    }

    private static void AssertUnchanged(TestAggregate aggregate)
    {
        aggregate.Version.Should().Be(1);
        aggregate.CreatedBy.Should().BeEmpty();
        aggregate.LastModifiedBy.Should().BeEmpty();
        aggregate.IsDeleted.Should().BeFalse();
    }

    public sealed record TestCommand : ICommand<Guid>;

    public sealed class TestAggregate(Guid id) : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id))
    {
        public void RaiseEvent(IDomainEvent domainEvent) => AddDomainEvent(domainEvent);
    }

    public sealed class TestDomainEvent : DomainEventBase;

    private sealed class ConfigurableBatchCommandHandler
        : BatchAggregateCommandHandlerBase<TestCommand, Guid, TestAggregate>
    {
        private readonly FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>> _executionResult;

        public ConfigurableBatchCommandHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IUnitOfWork unitOfWork,
            IEnumerable<TestAggregate> aggregateRoots,
            FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>> executionResult,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> beforeSaveProcessors,
            IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>> beforeSaveDeltaProcessors)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors, beforeSaveDeltaProcessors)
        {
            AggregateRoots = aggregateRoots.ToDictionary(aggregate => aggregate.Id);
            _executionResult = executionResult;
        }

        public int AggregateRootsCount => AggregateRoots.Count;

        protected override Task<FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>>> ExecuteAsync(
            TestCommand request,
            CancellationToken cancellationToken)
            => Task.FromResult(_executionResult);
    }
}
