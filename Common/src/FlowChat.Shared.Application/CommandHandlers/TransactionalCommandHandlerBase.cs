using FlowChat.Core.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        catch (DbUpdateException exception)
        {
            // Keep this outside the unit of work so EF execution strategies can finish all retries before application-specific recovery runs
            return await OnDbUpdateExceptionAfterRollbackAsync(request, exception, cancellationToken);
        }
        catch (Exception exception)
        {
            return await HandleUnexpectedExceptionAsync(exception);
        }
    }

    protected abstract Task<FlowChatResult<TValue>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected virtual Task<FlowChatResult<TValue>> OnDbUpdateExceptionAfterRollbackAsync(
        TCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        return HandleUnexpectedExceptionAsync(exception);
    }

    private static Task<FlowChatResult<TValue>> HandleUnexpectedExceptionAsync(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }
}
