namespace FlowChat.Shared.Domain;

public interface IVersionedEntity
{
    int Version { get; }
    void IncrementVersion();
}
