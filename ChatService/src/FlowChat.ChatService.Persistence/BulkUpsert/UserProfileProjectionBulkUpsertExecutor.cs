using EFCore.BulkExtensions;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Persistence.BulkUpsert;

public sealed class UserProfileProjectionBulkUpsertExecutor(AppDbContext dbContext)
    : IBulkUpsertExecutor<UserProfileProjectionDto>
{
    public async Task<FlowChatResult<BulkUpsertCommandResult>> UpsertAsync(
        IReadOnlyCollection<UserProfileProjectionDto> items,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            return FlowChatResult<BulkUpsertCommandResult>.Success(BulkUpsertCommandResult.Empty);
        }

        var entities = items.Select(item => new UserProfileProjectionEntity
        {
            UserId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId,
            DisplayName = item.DisplayName,
            AvatarUrl = item.AvatarUrl,
            CreatedBy = item.CreatedBy,
            CreatedAtUtc = item.CreatedAtUtc,
            LastModifiedBy = item.LastModifiedBy,
            LastModifiedAtUtc = item.LastModifiedAtUtc
        }).ToList();

        await dbContext.BulkInsertOrUpdateAsync(entities, cancellationToken: cancellationToken);

        return FlowChatResult<BulkUpsertCommandResult>.Success(
            BulkUpsertCommandResult.FromRequestedCount(items.Count));
    }
}
