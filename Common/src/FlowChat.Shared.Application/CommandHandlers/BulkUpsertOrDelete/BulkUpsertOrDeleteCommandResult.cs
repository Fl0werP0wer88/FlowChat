namespace FlowChat.Shared.Application;

public sealed record BulkUpsertOrDeleteCommandResult(
    int RequestedCount,
    int UpsertedCount,
    int DeletedCount)
{
    public static BulkUpsertOrDeleteCommandResult Empty { get; } = new(0, 0, 0);
}
