using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public abstract class AggregateRootUpsertCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    : FetchingAggregateRootCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    protected AggregateRootUpsertCommandHandlerBaseV3(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessorV2<TCommand, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
    {
    }

    protected static FlowChatResult<AggregateMutation<TResponse>> Created(TResponse response) =>
        Mutation(MutationType.Created, response);

    protected static FlowChatResult<AggregateMutation<TResponse>> Updated(TResponse response) =>
        Mutation(MutationType.Updated, response);
}
