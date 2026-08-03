using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public sealed class ProjectionSingleCommandHandler<TValue>(
    IUnitOfWork unitOfWork,
    IProjectionSingleRepository<TValue> repository)
    : TransactionalCommandHandlerBase<ProjectionSingleCommand<TValue>, Unit>(unitOfWork)
    where TValue : class
{
    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        ProjectionSingleCommand<TValue> request,
        CancellationToken cancellationToken)
    {
        await repository.UpsertOrSoftDeleteAsync(request.Item, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
