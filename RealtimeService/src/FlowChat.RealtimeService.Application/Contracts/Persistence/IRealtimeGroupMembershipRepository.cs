using FlowChat.RealtimeService.Domain.Enums;

namespace FlowChat.RealtimeService.Application.Contracts.Persistence;

public interface IRealtimeGroupMembershipRepository
{
    Task<IReadOnlyList<RealtimeGroupMembershipDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}
