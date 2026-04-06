using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class UserProfileProjectionWriteRepository(AppDbContext dbContext) : IUserProfileProjectionWriteRepository
{
    private const string ProjectionSource = "user-profile-events";
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<bool> InsertAsync(UserProfileProjection projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var exists = await _dbContext.UserProfileProjections
            .AnyAsync(x => x.UserProfileId == projection.UserProfileId, cancellationToken);

        if (exists)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;

        await _dbContext.UserProfileProjections.AddAsync(
            new UserProfileProjectionEntity
            {
                UserProfileId = projection.UserProfileId,
                FriendlyUserId = projection.FriendlyUserId,
                DisplayName = projection.DisplayName,
                FirstName = projection.FirstName,
                LastName = projection.LastName,
                Organization = projection.Organization,
                MainEmail = projection.MainEmail,
                MainPhone = projection.MainPhone,
                AvatarUrl = projection.AvatarUrl,
                Bio = projection.Bio,
                IsActive = projection.IsActive,
                LastSeenAtUtc = projection.LastSeenAtUtc,
                IsEmailVisible = projection.IsEmailVisible,
                IsPhoneVisible = projection.IsPhoneVisible,
                CreatedBy = ProjectionSource,
                CreatedAtUtc = now,
                LastModifiedBy = ProjectionSource,
                LastModifiedAtUtc = now
            },
            cancellationToken);

        return true;
    }

    public async Task<bool> UpdateAsync(UserProfileProjection projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        var entity = await _dbContext.UserProfileProjections
            .FirstOrDefaultAsync(x => x.UserProfileId == projection.UserProfileId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.FriendlyUserId = projection.FriendlyUserId;
        entity.DisplayName = projection.DisplayName;
        entity.FirstName = projection.FirstName;
        entity.LastName = projection.LastName;
        entity.Organization = projection.Organization;
        entity.MainEmail = projection.MainEmail;
        entity.MainPhone = projection.MainPhone;
        entity.AvatarUrl = projection.AvatarUrl;
        entity.Bio = projection.Bio;
        entity.IsActive = projection.IsActive;
        entity.LastSeenAtUtc = projection.LastSeenAtUtc;
        entity.IsEmailVisible = projection.IsEmailVisible;
        entity.IsPhoneVisible = projection.IsPhoneVisible;
        entity.LastModifiedBy = ProjectionSource;
        entity.LastModifiedAtUtc = DateTimeOffset.UtcNow;

        return true;
    }
}
