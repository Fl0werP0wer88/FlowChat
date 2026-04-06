using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class UserProfileProjectionReadRepository(AppDbContext dbContext) : IUserProfileProjectionReadRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    private static readonly Expression<Func<UserProfileProjectionEntity, UserProfileProjection>> Projection = entity => new(
        entity.UserProfileId,
        entity.FriendlyUserId,
        entity.DisplayName,
        entity.MainEmail,
        entity.MainPhone,
        entity.AvatarUrl,
        entity.Bio,
        entity.IsActive,
        entity.LastSeenAtUtc,
        entity.IsEmailVisible,
        entity.IsPhoneVisible,
        entity.FirstName,
        entity.LastName,
        entity.Organization);

    public async Task<IReadOnlyList<UserProfileProjection>> SearchAsync(
        string? firstName,
        string? lastName,
        string? organization,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UserProfileProjections
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            var firstNamePattern = $"{firstName}%";
            query = query.Where(entity => entity.FirstName != null && EF.Functions.Like(entity.FirstName, firstNamePattern));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            var lastNamePattern = $"{lastName}%";
            query = query.Where(entity => entity.LastName != null && EF.Functions.Like(entity.LastName, lastNamePattern));
        }

        if (!string.IsNullOrWhiteSpace(organization))
        {
            var organizationPattern = $"{organization}%";
            query = query.Where(entity => entity.Organization != null && EF.Functions.Like(entity.Organization, organizationPattern));
        }

        return await query
            .OrderBy(entity => entity.DisplayName)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }
}
