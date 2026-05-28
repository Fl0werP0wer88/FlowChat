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

        var now = DateTimeOffset.UtcNow;
        var entities = items.Select(item => new UserProfileProjectionEntity
        {
            UserId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId,
            FirstName = item.FirstName,
            LastName = item.LastName,
            AvatarUrl = item.AvatarUrl,
            CreatedBy = item.Source,
            CreatedAtUtc = now,
            LastModifiedBy = item.Source,
            LastModifiedAtUtc = now
        }).ToList();

        await dbContext.BulkInsertOrUpdateAsync(
            entities,
            new BulkConfig
            {
                PropertiesToExcludeOnUpdate =
                [
                    nameof(UserProfileProjectionEntity.CreatedBy),
                    nameof(UserProfileProjectionEntity.CreatedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);

        return FlowChatResult<BulkUpsertCommandResult>.Success(
            BulkUpsertCommandResult.FromRequestedCount(items.Count));
    }
}
