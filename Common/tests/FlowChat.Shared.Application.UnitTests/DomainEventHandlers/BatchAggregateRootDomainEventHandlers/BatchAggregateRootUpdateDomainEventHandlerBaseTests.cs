using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Application.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Application.UnitTests.DomainEventHandlers.BatchAggregateRootDomainEventHandlers;

public sealed class BatchAggregateRootUpdateDomainEventHandlerBaseTests
{
    [Fact]
    public void UpdateBatch_WithAggregateIds_ReturnsUpdatedMutationsInInputOrder()
    {
        var firstId = Id<TestAggregate>.FromGuid(Guid.NewGuid());
        var secondId = Id<TestAggregate>.FromGuid(Guid.NewGuid());
        IReadOnlyList<Id<TestAggregate>> aggregateIds = [firstId, secondId, firstId];

        var result = TestHandler.InvokeUpdateBatch(aggregateIds);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchOperationType.Should().Be(BatchOperationType.Updated);
        result.Value.Mutations.Select(mutation => mutation.Id).Should().Equal(aggregateIds);
        result.Value.Mutations.Should().OnlyContain(mutation => mutation.MutationType == MutationType.Updated);
    }

    [Fact]
    public void UpdateBatch_WithEmptyAggregateIds_ReturnsSuccessWithoutMutations()
    {
        var result = TestHandler.InvokeUpdateBatch([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchOperationType.Should().Be(BatchOperationType.Updated);
        result.Value.Mutations.Should().BeEmpty();
    }

    [Fact]
    public void UpdateBatch_WithNullAggregateIds_ThrowsArgumentNullException()
    {
        var action = () => TestHandler.InvokeUpdateBatch(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("aggregateIds");
    }

    private abstract class TestHandler
        : BatchAggregateRootUpdateDomainEventHandlerBase<TestNotification, TestAggregate>
    {
        protected TestHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>> beforeSaveProcessors,
            IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>> beforeSaveDeltaProcessors)
            : base(localEventsDispatcher, beforeSaveProcessors, beforeSaveDeltaProcessors)
        {
        }

        public static FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>> InvokeUpdateBatch(
            IReadOnlyList<Id<TestAggregate>> aggregateIds)
            => Success(aggregateIds, new DeltaProjectionMetadataV2(
                Guid.NewGuid(), 1));
    }

    private sealed class TestNotification : DomainEventBase;

    private sealed class TestAggregate(Guid id)
        : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id));
}
