using FlowChat.RealtimeService.Domain.Enums;

namespace FlowChat.RealtimeService.Application.Contracts.Persistence;

public interface IRealtimeGroupMembershipReadModelRepository
{
    Task<IReadOnlyList<RealtimeGroupMembershipReadModelDto>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetUserIdsByResourceIdAsync(
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<Guid> userIds,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task RemoveRangeAsync(
        IReadOnlyCollection<Guid> userIds,
        RealtimeGroupType groupType,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}
