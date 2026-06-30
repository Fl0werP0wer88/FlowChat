namespace FlowChat.Shared.Persistance;

public abstract class ReadEntityBase : IDeletableReadEntity
{
    public virtual DateTimeOffset? DeletedAt { get; set; }
}
