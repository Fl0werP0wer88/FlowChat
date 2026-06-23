using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class UserProfileProjectionReadRepository(AppDbContext dbContext) : IUserProfileProjectionReadRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    private static readonly Expression<Func<UserProfileProjectionEntity, UserProfileProjectionDto>> Projection = entity => new UserProfileProjectionDto
    {
        UserProfileId = entity.UserProfileId,
        FriendlyUserId = entity.FriendlyUserId,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        Organization = entity.Organization,
        MainEmail = entity.MainEmail == null
            ? null
            : new UserProfileProjectionEmailDto
            {
                Address = entity.MainEmail,
                IsConfirmed = entity.MainEmailIsConfirmed ?? false,
                IsVisible = entity.MainEmailIsVisible ?? false
            },
        MainPhone = entity.MainPhone == null
            ? null
            : new UserProfileProjectionPhoneDto
            {
                Number = entity.MainPhone,
                IsConfirmed = entity.MainPhoneIsConfirmed ?? false,
                IsVisible = entity.MainPhoneIsVisible ?? false
            },
        AvatarUrl = entity.AvatarUrl,
        Bio = entity.Bio,
        IsActive = entity.IsActive,
        LastSeenAtUtc = entity.LastSeenAtUtc
    };

    public async Task<UserProfileProjectionDto?> GetByUserProfileIdAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userProfileId, Guid.Empty);

        return await _dbContext.UserProfileProjections
            .AsNoTracking()
            .Where(entity => entity.UserProfileId == userProfileId)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserProfileProjectionDto?> GetByFriendlyUserIdAsync(
        string friendlyUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(friendlyUserId);

        return await _dbContext.UserProfileProjections
            .AsNoTracking()
            .Where(entity => entity.FriendlyUserId == friendlyUserId)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserProfileProjectionDto?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await _dbContext.UserProfileProjections
            .AsNoTracking()
            .Where(entity => entity.MainEmail != null && entity.MainEmail.ToLower() == normalizedEmail)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }

}


