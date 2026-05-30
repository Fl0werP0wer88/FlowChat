namespace FlowChat.Shared.Application;

public sealed class SnapshotApplicationEvent<TSnapshot>(TSnapshot value) : IApplicationEvent
{
    public TSnapshot Value { get; } = value;
}
