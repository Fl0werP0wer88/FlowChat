using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class UserProfileProjectionWriteRepository(AppDbContext dbContext)
    : IUserProfileProjectionWriteRepository
{
    public async Task InsertAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        await dbContext.UserProfileProjections.AddAsync(
            new UserProfileProjectionEntity
            {
                UserId = projection.UserProfileId,
                FriendlyUserId = projection.FriendlyUserId,
                DisplayName = projection.DisplayName,
                AvatarUrl = projection.AvatarUrl,
                CreatedBy = projection.CreatedBy,
                CreatedAtUtc = projection.CreatedAtUtc,
                LastModifiedBy = projection.LastModifiedBy,
                LastModifiedAtUtc = projection.LastModifiedAtUtc
            },
            cancellationToken);
    }

    public async Task<bool> UpdateAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var entity = await dbContext.UserProfileProjections
            .FirstOrDefaultAsync(x => x.UserId == projection.UserProfileId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.FriendlyUserId = projection.FriendlyUserId;
        entity.DisplayName = projection.DisplayName;
        entity.AvatarUrl = projection.AvatarUrl;
        entity.CreatedBy = projection.CreatedBy;
        entity.CreatedAtUtc = projection.CreatedAtUtc;
        entity.LastModifiedBy = projection.LastModifiedBy;
        entity.LastModifiedAtUtc = projection.LastModifiedAtUtc;

        return true;
    }
}
