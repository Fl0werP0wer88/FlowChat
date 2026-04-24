using FlowChat.PresenceService.Application.Contracts.Persistence;
using FlowChat.PresenceService.Domain.Entities.UserPresencePreferences;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.PresenceService.Persistence.Repositories;

public sealed class UserPresencePreferencesWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<UserPresencePreferences>(dbContext), IUserPresencePreferencesWriteRepository
{
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public override async Task<UserPresencePreferences?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var typedId = Id<UserPresencePreferences>.FromGuid(id);

        return await _dbContext.UserPresencePreferences
            .FirstOrDefaultAsync(x => x.Id == typedId, cancellationToken);
    }
}
