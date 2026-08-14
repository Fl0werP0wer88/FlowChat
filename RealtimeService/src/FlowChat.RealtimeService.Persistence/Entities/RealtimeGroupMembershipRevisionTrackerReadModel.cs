namespace FlowChat.RealtimeService.Persistence.Entities;

public sealed class RealtimeGroupMembershipRevisionTrackerReadModel
{
    private RealtimeGroupMembershipRevisionTrackerReadModel()
    {
    }

    public Guid ConversationId { get; private set; }

    public int Revision { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
