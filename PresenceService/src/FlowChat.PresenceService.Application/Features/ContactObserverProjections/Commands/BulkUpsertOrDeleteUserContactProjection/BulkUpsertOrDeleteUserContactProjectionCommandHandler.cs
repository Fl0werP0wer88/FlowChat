using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;

public sealed class BulkUpsertOrDeleteUserContactProjectionCommandHandler
    : TransactionalCommandHandlerBase<BulkUpsertOrDeleteUserContactProjectionCommand, Unit>
{
    private readonly IContactObserverProjectionBulkRepository _bulkRepository;

    public BulkUpsertOrDeleteUserContactProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IContactObserverProjectionBulkRepository bulkRepository)
        : base(unitOfWork)
    {
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
    }

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        BulkUpsertOrDeleteUserContactProjectionCommand request,
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
