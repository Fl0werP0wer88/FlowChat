using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.ChatService.Persistence.Repositories;

public sealed class UserProfileProjectionReadRepository : IUserProfileProjectionReadRepository
{
    private readonly AppDbContext _dbContext;

    public UserProfileProjectionReadRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IReadOnlyList<ConversationParticipantDto>> GetByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();

        return await _dbContext.UserProfileProjections
            .AsNoTracking()
            .Where(x => ids.Contains(x.UserId))
            .Select(x => new ConversationParticipantDto(
                x.UserId,
                x.DisplayName,
                x.AvatarUrl,
                x.UserId))
            .ToListAsync(cancellationToken);
    }
}
