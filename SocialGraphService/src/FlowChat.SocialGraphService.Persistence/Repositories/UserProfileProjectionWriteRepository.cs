using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class UserProfileProjectionWriteRepository(AppDbContext dbContext) : IUserProfileProjectionWriteRepository
{
    private const string ProjectionSource = "user-profile-events";
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task InsertAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var now = DateTimeOffset.UtcNow;

        await _dbContext.UserProfileProjections.AddAsync(
            new UserProfileProjectionEntity
            {
                UserProfileId = projection.UserProfileId,
                FriendlyUserId = projection.FriendlyUserId,
                FirstName = projection.FirstName,
                LastName = projection.LastName,
                Organization = projection.Organization,
                MainEmail = projection.MainEmail?.Address,
                MainEmailIsConfirmed = projection.MainEmail?.IsConfirmed,
                MainEmailIsVisible = projection.MainEmail?.IsVisible,
                MainPhone = projection.MainPhone?.Number,
                MainPhoneIsConfirmed = projection.MainPhone?.IsConfirmed,
                MainPhoneIsVisible = projection.MainPhone?.IsVisible,
                AvatarUrl = projection.AvatarUrl,
                Bio = projection.Bio,
                IsActive = projection.IsActive,
                LastSeenAtUtc = projection.LastSeenAtUtc,
                CreatedBy = ProjectionSource,
                CreatedAtUtc = now,
                LastModifiedBy = ProjectionSource,
                LastModifiedAtUtc = now
            },
            cancellationToken);
    }


    public async Task<bool> UpdateAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var entity = await _dbContext.UserProfileProjections
            .FirstOrDefaultAsync(x => x.UserProfileId == projection.UserProfileId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.FriendlyUserId = projection.FriendlyUserId;
        entity.FirstName = projection.FirstName;
        entity.LastName = projection.LastName;
        entity.Organization = projection.Organization;
        entity.MainEmail = projection.MainEmail?.Address;
        entity.MainEmailIsConfirmed = projection.MainEmail?.IsConfirmed;
        entity.MainEmailIsVisible = projection.MainEmail?.IsVisible;
        entity.MainPhone = projection.MainPhone?.Number;
        entity.MainPhoneIsConfirmed = projection.MainPhone?.IsConfirmed;
        entity.MainPhoneIsVisible = projection.MainPhone?.IsVisible;
        entity.AvatarUrl = projection.AvatarUrl;
        entity.Bio = projection.Bio;
        entity.IsActive = projection.IsActive;
        entity.LastSeenAtUtc = projection.LastSeenAtUtc;
        entity.LastModifiedBy = ProjectionSource;
        entity.LastModifiedAtUtc = DateTimeOffset.UtcNow;

        return true;
    }
}


