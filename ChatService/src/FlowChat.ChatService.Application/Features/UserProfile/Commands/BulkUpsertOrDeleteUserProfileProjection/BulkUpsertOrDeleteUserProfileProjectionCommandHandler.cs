using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionCommandHandler
    : TransactionalCommandHandlerBase<BulkUpsertOrDeleteUserProfileProjectionCommand, BulkUpsertOrDeleteCommandResult>
{
    private readonly IUserProfileProjectionBulkRepository _bulkRepository;

    public BulkUpsertOrDeleteUserProfileProjectionCommandHandler(
        IUnitOfWork unitOfWork,
        IUserProfileProjectionBulkRepository bulkRepository)
        : base(unitOfWork)
    {
        _bulkRepository = bulkRepository ?? throw new ArgumentNullException(nameof(bulkRepository));
    }

    protected override Task<FlowChatResult<BulkUpsertOrDeleteCommandResult>> HandleInTransactionAsync(
        BulkUpsertOrDeleteUserProfileProjectionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Items.Count == 0)
            return Task.FromResult(FlowChatResult<BulkUpsertOrDeleteCommandResult>.Success(BulkUpsertOrDeleteCommandResult.Empty));

        return _bulkRepository.BulkUpsertOrDeleteAsync(request.Items, cancellationToken);
    }
}

