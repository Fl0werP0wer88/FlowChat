using FlowChat.RealtimeService.Domain.Enums;

namespace FlowChat.RealtimeService.Persistence.Entities;

public sealed class RealtimeGroupMembershipReadModel
{
    private RealtimeGroupMembershipReadModel()
    {
    }

    public Guid UserId { get; private set; }

    public RealtimeGroupType GroupType { get; private set; }

    public Guid ResourceId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static RealtimeGroupMembershipReadModel Create(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(resourceId, Guid.Empty);

        return new RealtimeGroupMembershipReadModel
        {
            UserId = userId,
            GroupType = groupType,
            ResourceId = resourceId,
            CreatedAt = createdAt
        };
    }
}
