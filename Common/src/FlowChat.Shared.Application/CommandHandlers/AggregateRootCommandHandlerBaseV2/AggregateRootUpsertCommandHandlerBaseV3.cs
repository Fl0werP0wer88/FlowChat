using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;

public abstract class AggregateRootUpsertCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    : AggregateRootCommandHandlerBaseV3<TCommand, TResponse, TAggregate>
    where TCommand : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
    where TAggregate : class, IAggregateRoot
{
    protected AggregateRootUpsertCommandHandlerBaseV3(
        ILocalEventDispatcher localEventsDispatcher,
        IUnitOfWork unitOfWork,
        IEnumerable<IAggregateBeforeSaveProcessor<TCommand, TAggregate>> beforeSaveProcessors)
        : base(localEventsDispatcher, unitOfWork, beforeSaveProcessors)
    {
    }

    protected void SetInserted() => SetMutationType(MutationType.Created);

    protected void SetUpdated() => SetMutationType(MutationType.Updated);
}
