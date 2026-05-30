namespace FlowChat.Shared.Application;

public interface ISnapshotApplicationEvent<TSnapshot> : IApplicationEvent
{
    TSnapshot Value { get; }
}
