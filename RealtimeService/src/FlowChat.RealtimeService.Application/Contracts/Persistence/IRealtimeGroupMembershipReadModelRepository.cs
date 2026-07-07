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
