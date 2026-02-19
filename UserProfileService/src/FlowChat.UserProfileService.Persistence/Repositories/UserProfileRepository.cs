using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public class UserProfileRepository : RepositoryBase<UserProfile>, IUserProfileRepository
{
    public UserProfileRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<UserProfile>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.UserProfiles
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.UserName)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.Trim().ToLower();

        return await DbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName.ToLower() == normalizedUserName, cancellationToken);
    }

    public async Task<bool> UserNameExistsAsync(
        string userName,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.Trim().ToLower();

        return await DbContext.UserProfiles
            .AnyAsync(
                x => (!excludedUserId.HasValue || x.Id != excludedUserId.Value)
                     && x.UserName.ToLower() == normalizedUserName,
                cancellationToken);
    }
}
