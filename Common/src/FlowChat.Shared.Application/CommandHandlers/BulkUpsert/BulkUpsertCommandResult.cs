namespace FlowChat.Shared.Application;

public sealed record BulkUpsertCommandResult(
    int RequestedCount,
    int UpsertedCount)
{
    public static BulkUpsertCommandResult Empty { get; } = new(0, 0);

    public static BulkUpsertCommandResult FromRequestedCount(int requestedCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(requestedCount);

        return new BulkUpsertCommandResult(requestedCount, requestedCount);
    }
}
