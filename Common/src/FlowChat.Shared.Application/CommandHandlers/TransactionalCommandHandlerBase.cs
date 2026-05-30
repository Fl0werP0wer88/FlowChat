using FlowChat.Core.Results;
using MediatR;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FlowChat.Shared.Application;

public abstract class TransactionalCommandHandlerBase<TCommand, TValue>
    : ICommandHandler<TCommand, TValue>
    where TCommand : ICommand<TValue>, IRequest<FlowChatResult<TValue>>
    where TValue : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    protected TransactionalCommandHandlerBase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<FlowChatResult<TValue>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.ExecuteCommandInTransactionAsync(
                token => HandleInTransactionAsync(request, token),
                cancellationToken);
        }
        catch (Exception exception)
        {
            return await HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }
    }

    protected abstract Task<FlowChatResult<TValue>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected virtual Task<FlowChatResult<TValue>> HandleUnexpectedExceptionAsync(
        TCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }
}
