namespace FlowChat.RealtimeService.Persistence.Entities;

public sealed class RealtimeGroupMembershipVersionTrackerReadModel
{
    private RealtimeGroupMembershipVersionTrackerReadModel()
    {
    }

    public Guid ConversationId { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static RealtimeGroupMembershipVersionTrackerReadModel Create(
        Guid conversationId,
        int version,
        DateTimeOffset updatedAt)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(conversationId, Guid.Empty);

        return new RealtimeGroupMembershipVersionTrackerReadModel
        {
            ConversationId = conversationId,
            Version = version,
            UpdatedAt = updatedAt
        };
    }

    public void UpdateVersion(int version, DateTimeOffset updatedAt)
    {
        Version = version;
        UpdatedAt = updatedAt;
    }
}
