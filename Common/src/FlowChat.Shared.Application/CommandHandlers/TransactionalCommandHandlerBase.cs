using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public abstract class TransactionalCommandHandlerBase<TCommand, TValue>
    : CommandHandlerBase<TCommand, TValue>
    where TCommand : ICommand<TValue>, IRequest<FlowChatResult<TValue>>
    where TValue : notnull
{
    private readonly IUnitOfWork _unitOfWork;

    protected TransactionalCommandHandlerBase(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    protected sealed override Task<FlowChatResult<TValue>> HandleCommandAsync(
        TCommand request,
        CancellationToken cancellationToken)
        => _unitOfWork.ExecuteCommandInTransactionAsync(
            async token =>
            {
                var result = await HandleInTransactionAsync(request, token);

                if (result.IsSuccess && _unitOfWork is IConsumedOffsetCommitter offsetCommitter)
                {
                    await offsetCommitter.CommitConsumedOffsetsAsync(token);
                }

                return result;
            },
            cancellationToken);

    protected abstract Task<FlowChatResult<TValue>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken);
}
