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
        string firstName,
        string lastName,
        string organization,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(organization);

        var firstNamePattern = $"{firstName}%";
        var lastNamePattern = $"{lastName}%";
        var organizationPattern = $"{organization}%";

        return await _dbContext.UserProfileProjections
            .AsNoTracking()
            .Where(entity => entity.FirstName != null && EF.Functions.Like(entity.FirstName, firstNamePattern))
            .Where(entity => entity.LastName != null && EF.Functions.Like(entity.LastName, lastNamePattern))
            .Where(entity => entity.Organization != null && EF.Functions.Like(entity.Organization, organizationPattern))
            .OrderBy(entity => entity.DisplayName)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }
}
