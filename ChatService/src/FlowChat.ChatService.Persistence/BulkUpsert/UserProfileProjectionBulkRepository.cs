using EFCore.BulkExtensions;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;

namespace FlowChat.ChatService.Persistence.BulkUpsert;

public sealed class UserProfileProjectionBulkRepository(AppDbContext dbContext)
    : IBulkRepository<UserProfileProjectionDto>
{
    public async Task<FlowChatResult<int>> BulkUpsertAsync(
        IReadOnlyCollection<UserProfileProjectionDto> items,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var entities = items.Select(item => new UserProfileProjectionEntity
        {
            UserId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId,
            DisplayName = item.DisplayName,
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

        return FlowChatResult<int>.Success(items.Count);
    }

    public async Task<FlowChatResult<int>> BulkDeleteAsync(
        IReadOnlyCollection<Id<UserProfileProjectionDto>> ids,
        CancellationToken cancellationToken)
    {
        var guids = ids.Select(id => id.Value).ToList();

        var entities = dbContext.UserProfileProjections
            .Where(e => guids.Contains(e.UserId))
            .ToList();

        await dbContext.BulkDeleteAsync(entities, cancellationToken: cancellationToken);

        return FlowChatResult<int>.Success(ids.Count);
    }
}
