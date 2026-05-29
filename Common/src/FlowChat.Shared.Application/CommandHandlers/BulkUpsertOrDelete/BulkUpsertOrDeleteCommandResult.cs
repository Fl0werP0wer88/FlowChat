using FlowChat.Shared.Domain;
using Ardalis.SmartEnum;

namespace FlowChat.Shared.Application;

public sealed record BulkUpsertOrDeleteCommandResult(
    int RequestedCount,
    int UpsertedCount,
    int DeletedCount)
{
    public static BulkUpsertOrDeleteCommandResult Empty { get; } = new(0, 0, 0);
}

public sealed record BulkUpsertOrDeleteCommandResult<TValue>(IReadOnlyCollection<BulkUpsertOrDeleteCommandResultRow<TValue>> Results);

public sealed record BulkUpsertOrDeleteCommandResultRow<TValue>(Id<TValue> EntityId, UpsertOrDeleteStatus Status);


public sealed record UpsertOrDeleteFailure(bool IsTransient, string FailureDescription);

public sealed class UpsertOrDeleteStatus(string name, int value, UpsertOrDeleteFailure? failure)
    : SmartEnum<UpsertOrDeleteStatus>(name, value)
{
    public static readonly UpsertOrDeleteStatus Updated             = new("Updated",  0, null);
    public static readonly UpsertOrDeleteStatus Inserted            = new("Inserted", 1, null);
    public static readonly UpsertOrDeleteStatus Deleted             = new("Deleted",  2, null);
    public static readonly UpsertOrDeleteStatus TransientFailure    = new("TransientFailure",    3, new(true,  "Transient failure"));
    public static readonly UpsertOrDeleteStatus NonTransientFailure = new("NonTransientFailure", 4, new(false, "Non-transient failure"));

    public UpsertOrDeleteFailure? Failure { get; } = failure;
}


