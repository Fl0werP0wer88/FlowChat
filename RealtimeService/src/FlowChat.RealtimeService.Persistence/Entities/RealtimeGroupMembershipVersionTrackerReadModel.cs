namespace FlowChat.RealtimeService.Persistence.Entities;

public sealed class RealtimeGroupMembershipVersionTrackerReadModel
{
    private RealtimeGroupMembershipVersionTrackerReadModel()
    {
    }

    public Guid ConversationId { get; private set; }

    public int Version { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
