using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public abstract class AggregateRootUpdateCommandHandlerBaseV2<TCommand, TResponse, TAggregate>
    : AggregateRootCommandHandlerBaseV2<TCommand, TResponse, TAggregate>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    protected AggregateRootUpdateCommandHandlerBaseV2(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<TCommand, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
    {
    }

    protected override AggregateState GetAggregateState(TCommand request, TAggregate aggregateRoot) =>
        AggregateState.Updated;
}
