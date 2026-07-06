using FlowChat.RealtimeService.Domain.Enums;

namespace FlowChat.RealtimeService.Persistence.Entities;

public sealed class RealtimeGroupMembership
{
    private RealtimeGroupMembership()
    {
    }

    public Guid UserId { get; private set; }

    public RealtimeGroupType GroupType { get; private set; }

    public Guid ResourceId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static RealtimeGroupMembership Create(
        Guid userId,
        RealtimeGroupType groupType,
        Guid resourceId,
        DateTimeOffset createdAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(userId, Guid.Empty);
        ArgumentOutOfRangeException.ThrowIfEqual(resourceId, Guid.Empty);

        return new RealtimeGroupMembership
        {
            UserId = userId,
            GroupType = groupType,
            ResourceId = resourceId,
            CreatedAt = createdAt
        };
    }
}
