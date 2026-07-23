using FluentAssertions;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;
using FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.Shared.Application.UnitTests.CommandHandlers.BatchAggregateCommandHandlerBase;

public sealed class BatchAggregateRemoveCommandHandlerBaseTests
{
    [Fact]
    public void RemoveBatch_WithAggregateIds_ReturnsDeletedMutationsInInputOrder()
    {
        var response = Guid.NewGuid();
        var firstId = Id<TestAggregate>.FromGuid(Guid.NewGuid());
        var secondId = Id<TestAggregate>.FromGuid(Guid.NewGuid());
        IReadOnlyList<Id<TestAggregate>> aggregateIds = [firstId, secondId, firstId];

        var result = TestHandler.InvokeRemoveBatch(response, aggregateIds);

        result.IsSuccess.Should().BeTrue();
        result.Value.Response.Should().Be(response);
        result.Value.BatchOperationType.Should().Be(BatchOperationType.Deleted);
        result.Value.Mutations.Select(mutation => mutation.Id).Should().Equal(aggregateIds);
        result.Value.Mutations.Should().OnlyContain(
            mutation => mutation.MutationType == MutationType.Deleted);
    }

    [Fact]
    public void RemoveBatch_WithEmptyAggregateIds_ReturnsSuccessWithoutMutations()
    {
        var response = Guid.NewGuid();

        var result = TestHandler.InvokeRemoveBatch(response, []);

        result.IsSuccess.Should().BeTrue();
        result.Value.Response.Should().Be(response);
        result.Value.BatchOperationType.Should().Be(BatchOperationType.Deleted);
        result.Value.Mutations.Should().BeEmpty();
    }

    [Fact]
    public void RemoveBatch_WithNullAggregateIds_ThrowsArgumentNullException()
    {
        var action = () => TestHandler.InvokeRemoveBatch(Guid.NewGuid(), null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("aggregateIds");
    }

    private abstract class TestHandler
        : BatchAggregateRemoveCommandHandlerBase<TestCommand, Guid, TestAggregate>
    {
        protected TestHandler(
            ILocalEventDispatcher localEventsDispatcher,
            IUnitOfWork unitOfWork,
            IEnumerable<IAggregateBeforeSaveProcessorV2<TestCommand, TestAggregate>> beforeSaveProcessors,
            IEnumerable<IAggregateBeforeSaveDeltaProcessorV2<TestCommand, TestAggregate>> beforeSaveDeltaProcessors)
            : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors, beforeSaveDeltaProcessors)
        {
        }

        public static FlowChatResult<BatchAggregateMutation<Guid, TestAggregate>> InvokeRemoveBatch(
            Guid response,
            IReadOnlyList<Id<TestAggregate>> aggregateIds)
            => RemoveBatch(response, aggregateIds, new DeltaProjectionMetadataV2(Guid.NewGuid(), 1));
    }

    private sealed record TestCommand : ICommand<Guid>;

    private sealed class TestAggregate(Guid id)
        : AggregateRootBase<TestAggregate>(Id<TestAggregate>.FromGuid(id));
}
