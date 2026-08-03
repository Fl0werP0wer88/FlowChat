using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public sealed class ProjectionCommandHandler<TValue>(
    IUnitOfWork unitOfWork,
    IProjectionRepository<TValue> repository)
    : TransactionalCommandHandlerBase<ProjectionCommand<TValue>, Unit>(unitOfWork)
    where TValue : class
{
    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        ProjectionCommand<TValue> request,
        CancellationToken cancellationToken)
    {
        await repository.UpsertOrSoftDeleteAsync(request.Item, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
