namespace FlowChat.Shared.Persistance;

public abstract class ReadEntityBase : IReadEntity
{
    public DateTimeOffset? DeletedAt { get; set; }
}
