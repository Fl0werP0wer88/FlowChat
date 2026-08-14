using FlowChat.Core.Results;
using MediatR;

namespace FlowChat.Shared.Application;

public abstract class ProjectionBulkCommandHandlerBase<TCommand, TItem, TRepository>
    : TransactionalCommandHandlerBase<TCommand, Unit>
    where TCommand : IProjectionBulkCommand<TItem>
    where TItem : notnull
    where TRepository : IProjectionBulkRepository<TItem>
{
    private readonly TRepository _bulkRepository;

    protected ProjectionBulkCommandHandlerBase(
        IUnitOfWork unitOfWork,
        TRepository bulkRepository)
        : base(unitOfWork)
    {
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
    }

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        TCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
        {
            return FlowChatResult<Unit>.Success(Unit.Value);
        }

        await _bulkRepository.BulkUpsertOrSoftDeleteAsync(request.Items, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
