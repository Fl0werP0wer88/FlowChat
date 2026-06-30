namespace FlowChat.Shared.Persistance;

public interface IDeletableReadEntity
{
    public DateTimeOffset? DeletedAt { get; set; }
}
