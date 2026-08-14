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

public sealed class BatchAggregateRootAddDomainEventHandlerBaseTests
{
    [Fact]
    public void AddBatch_WithAggregateIds_ReturnsCreatedMutationsInInputOrder()
    {
        var firstId = Id<TestAggregate>.FromGuid(Guid.NewGuid());
        var secondId = Id<TestAggregate>.FromGuid(Guid.NewGuid());
        IReadOnlyList<Id<TestAggregate>> aggregateIds = [firstId, secondId, firstId];

        var result = TestHandler.InvokeAddBatch(aggregateIds);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchOperationType.Should().Be(BatchOperationType.Created);
        result.Value.Mutations.Select(mutation => mutation.Id).Should().Equal(aggregateIds);
        result.Value.Mutations.Should().OnlyContain(mutation => mutation.MutationType == MutationType.Created);
    }

    [Fact]
    public void AddBatch_WithEmptyAggregateIds_ReturnsSuccessWithoutMutations()
    {
        var result = TestHandler.InvokeAddBatch([]);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchOperationType.Should().Be(BatchOperationType.Created);
        result.Value.Mutations.Should().BeEmpty();
    }

    [Fact]
    public void AddBatch_WithNullAggregateIds_ThrowsArgumentNullException()
    {
        var action = () => TestHandler.InvokeAddBatch(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("aggregateIds");
    }

    private abstract class TestHandler
        : BatchAggregateRootAddDomainEventHandlerBase<TestNotification, TestAggregate>
    {
        protected TestHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestNotification, TestAggregate>> beforeSaveProcessors,
            IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TestNotification, TestAggregate>> beforeSaveDeltaProcessors)
            : base(localEventsDispatcher, beforeSaveProcessors, beforeSaveDeltaProcessors)
        {
        }

        public static FlowChatResult<BatchAggregateDomainEventMutation<TestAggregate>> InvokeAddBatch(
            IReadOnlyList<Id<TestAggregate>> aggregateIds)
            => Success(aggregateIds, new DeltaProjectionMetadataV2(
                Guid.NewGuid(), 1));
    }

    private sealed class TestNotification : DomainEventBase;

    private sealed class TestAggregate(Guid id)
        : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id));
}
