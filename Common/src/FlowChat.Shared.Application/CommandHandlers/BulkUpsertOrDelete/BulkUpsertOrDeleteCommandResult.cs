using FlowChat.Shared.Domain;
using Ardalis.SmartEnum;

namespace FlowChat.Shared.Application;

public sealed record BulkUpsertOrDeleteCommandResult<TValue>(IReadOnlyCollection<BulkUpsertOrDeleteCommandResultItem<TValue>> Results);

public sealed record BulkUpsertOrDeleteCommandResultItem<TValue>(Id<TValue> EntityId, UpsertOrDeleteStatus Status);


public sealed record UpsertOrDeleteFailure(bool IsTransient, string FailureDescription);

public sealed class UpsertOrDeleteStatus(string name, int value, UpsertOrDeleteFailure? failure)
    : SmartEnum<UpsertOrDeleteStatus>(name, value)
{
    public static readonly UpsertOrDeleteStatus Processed = new("Processed", 0, null);
    public static readonly UpsertOrDeleteStatus TransientFailure = new("TransientFailure", 1, new(true, "Transient failure"));
    public static readonly UpsertOrDeleteStatus NonTransientFailure = new("NonTransientFailure", 2, new(false, "Non-transient failure"));

    public UpsertOrDeleteFailure? Failure { get; } = failure;
}


