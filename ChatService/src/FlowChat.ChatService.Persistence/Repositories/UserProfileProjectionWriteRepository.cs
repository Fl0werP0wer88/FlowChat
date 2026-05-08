using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class UserProfileProjectionWriteRepository(AppDbContext dbContext)
    : IUserProfileProjectionWriteRepository
{
    private const string ProjectionSource = "user-profile-events";

    public async Task InsertAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        await dbContext.UserProfileProjections.AddAsync(
            new UserProfileProjection
            {
                UserId = projection.UserProfileId,
                FriendlyUserId = projection.FriendlyUserId,
                DisplayName = projection.DisplayName,
                AvatarUrl = projection.AvatarUrl,
                UpdatedAtUtc = DateTimeOffset.UtcNow
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
        entity.UpdatedAtUtc = DateTimeOffset.UtcNow;

        return true;
    }
}
