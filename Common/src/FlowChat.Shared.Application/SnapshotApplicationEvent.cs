namespace FlowChat.Shared.Application;

public sealed class SnapshotApplicationEvent<TSnapshot>(TSnapshot value) : ISnapshotApplicationEvent<TSnapshot>
{
    public TSnapshot Value { get; } = value;
}
