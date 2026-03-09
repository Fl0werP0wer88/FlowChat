using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public class UserProfileRepository : RepositoryBase<UserProfile>, IUserProfileRepository
{
    public UserProfileRepository(AppDbContext dbContext) : base(dbContext)
    {
    }

    public override async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var typedId = Id<UserProfile>.FromGuid(id);

        return await DbContext.UserProfiles
            .AsNoTracking()
            .Include(x => x.Emails)
            .Include(x => x.Phones)
            .FirstOrDefaultAsync(x => x.Id == typedId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfile>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.UserProfiles
            .AsNoTracking()
            .Include(x => x.Emails)
            .Include(x => x.Phones)
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
            .Include(x => x.Emails)
            .Include(x => x.Phones)
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
                x => (!excludedUserId.HasValue || x.Id != Id<UserProfile>.FromGuid(excludedUserId.Value))
                     && x.UserName.ToLower() == normalizedUserName,
                cancellationToken);
    }
}
