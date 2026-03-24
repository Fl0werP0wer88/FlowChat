using FlowChat.Domain.Abstractions;
using FlowChat.Persistence.EntityFrameworkCore;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class UserProfileWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<UserProfile>(dbContext), IUserProfileWriteRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public override async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var typedId = Id<UserProfile>.FromGuid(id);

        return await _dbContext.UserProfiles
            .Include(x => x.Emails)
            .Include(x => x.Phones)
            .FirstOrDefaultAsync(x => x.Id == typedId, cancellationToken);
    }
}
