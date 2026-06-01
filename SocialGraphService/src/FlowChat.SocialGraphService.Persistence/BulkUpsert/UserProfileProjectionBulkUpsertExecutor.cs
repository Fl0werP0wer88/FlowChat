using EFCore.BulkExtensions;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.Entities;

namespace FlowChat.SocialGraphService.Persistence.BulkUpsert;

public sealed class UserProfileProjectionBulkUpsertExecutor(AppDbContext dbContext)
    : IBulkUpsertExecutor<UserProfileProjectionDto>
{
    private const string ProjectionSource = "user-profile-events";

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
        var entities = items.Select(item => new UserProfileReadModelEntity
        {
            UserProfileId = item.UserProfileId,
            FriendlyUserId = item.FriendlyUserId,
            FirstName = item.FirstName,
            LastName = item.LastName,
            Organization = item.Organization,
            MainEmail = item.MainEmail?.Address,
            MainEmailIsConfirmed = item.MainEmail?.IsConfirmed,
            MainEmailIsVisible = item.MainEmail?.IsVisible,
            MainPhone = item.MainPhone?.Number,
            MainPhoneIsConfirmed = item.MainPhone?.IsConfirmed,
            MainPhoneIsVisible = item.MainPhone?.IsVisible,
            AvatarUrl = item.AvatarUrl,
            Bio = item.Bio,
            IsActive = item.IsActive,
            LastSeenAtUtc = item.LastSeenAtUtc,
            CreatedBy = ProjectionSource,
            CreatedAtUtc = now,
            LastModifiedBy = ProjectionSource,
            LastModifiedAtUtc = now
        }).ToList();

        await dbContext.BulkInsertOrUpdateAsync(
            entities,
            new BulkConfig
            {
                PropertiesToExcludeOnUpdate =
                [
                    nameof(UserProfileReadModelEntity.CreatedBy),
                    nameof(UserProfileReadModelEntity.CreatedAtUtc)
                ]
            },
            cancellationToken: cancellationToken);

        return FlowChatResult<BulkUpsertCommandResult>.Success(
            BulkUpsertCommandResult.FromRequestedCount(items.Count));
    }
}
