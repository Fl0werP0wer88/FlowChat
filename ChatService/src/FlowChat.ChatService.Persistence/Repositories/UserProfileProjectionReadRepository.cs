using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.UserProfile;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class UserProfileProjectionReadRepository : IUserProfileProjectionReadRepository
{
    private readonly AppDbContext _dbContext;

    public UserProfileProjectionReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<UserProfileConversationParticipantDto>> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();

        return (await _dbContext.UserProfileProjections
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId) && x.DeletedAt == null)
            .Select(x => new { x.UserId, x.FirstName, x.LastName, x.AvatarUrl })
            .ToListAsync(cancellationToken))
            .Select(x => new UserProfileConversationParticipantDto(
                x.UserId,
                ComputeDisplayName(x.FirstName, x.LastName),
                x.AvatarUrl))
            .ToList();
    }

    private static string? ComputeDisplayName(string? firstName, string? lastName)
    {
        var parts = ((string?[]) [firstName, lastName]).Where(p => !string.IsNullOrEmpty(p));
        var name = string.Join(" ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }
}
