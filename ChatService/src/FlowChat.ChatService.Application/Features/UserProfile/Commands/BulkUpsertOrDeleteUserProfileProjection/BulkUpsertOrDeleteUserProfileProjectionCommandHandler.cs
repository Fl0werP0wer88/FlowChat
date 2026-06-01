using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionCommandHandler
    : TransactionalCommandHandlerBase<BulkUpsertOrDeleteUserProfileProjectionCommand, Unit>
{
    private readonly IUserProfileProjectionBulkRepository _bulkRepository;

    public BulkUpsertOrDeleteUserProfileProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IUserProfileProjectionBulkRepository bulkRepository)
        : base(unitOfWork)
    {
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
    }

    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        BulkUpsertOrDeleteUserProfileProjectionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            return FlowChatResult<Unit>.Success(Unit.Value);

        await _bulkRepository.BulkUpsertOrSoftDeleteAsync(request.Items, cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }
}
