using FlowChat.Core.Results;
using MediatR;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FlowChat.Shared.Application;

public abstract class CommandHandlerBase<TCommand, TValue>
    : ICommandHandler<TCommand, TValue>
    where TCommand : ICommand<TValue>, IRequest<FlowChatResult<TValue>>
    where TValue : notnull
{
    public async Task<FlowChatResult<TValue>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await HandleCommandAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            return await HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }
    }

    protected abstract Task<FlowChatResult<TValue>> HandleCommandAsync(
        TCommand request,
        CancellationToken cancellationToken);

    protected virtual Task<FlowChatResult<TValue>> HandleUnexpectedExceptionAsync(
        TCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Preserve the original stack trace when rethrowing from the async handler boundary
        ExceptionDispatchInfo.Capture(exception).Throw();
        throw new UnreachableException();
    }
}
