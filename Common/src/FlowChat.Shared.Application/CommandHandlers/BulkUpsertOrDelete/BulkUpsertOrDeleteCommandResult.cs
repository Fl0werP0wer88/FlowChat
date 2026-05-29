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



public sealed record BulkUpsertOrDeleteCommandResult<TValue>(
    int RequestedCount,
    int UpsertedCount,
    int DeletedCount)
{
    public static BulkUpsertOrDeleteCommandResult Empty { get; } = new(0, 0, 0);
}


public sealed record BulkUpsertOrDeleteCommandResultRow<TValue>(Id<TValue> EntityId, UpsertOrDeleteStatus Status);


public abstract class UpsertOrDeleteStatus(string name, int value)
    : SmartEnum<UpsertOrDeleteStatus>(name, value)
{
    public static readonly UpsertOrDeleteStatus Updated = new UpdatedEnum();
    public static readonly UpsertOrDeleteStatus Inserted = new InsertedEnum();
    public static readonly UpsertOrDeleteStatus Deleted = new DeletedEnum();
    public static readonly UpsertOrDeleteStatus TransientFailure = new TransientFailEnum();
    public static readonly UpsertOrDeleteStatus NonTransientFailure = new NonTransientFailEnum();

    public abstract bool IsFailure { get; }
    public abstract bool? IsTransient { get; }

    private sealed class UpdatedEnum : UpsertOrDeleteStatus
    {
        public UpdatedEnum() : base("Updated", 0) { }

        public override bool IsFailure => false;
        public override bool? IsTransient => null;
    }

    private sealed class InsertedEnum : UpsertOrDeleteStatus
    {
        public InsertedEnum() : base("Inserted", 1) { }

        public override bool IsFailure => false;
        public override bool? IsTransient => null;
    }

    private sealed class DeletedEnum : UpsertOrDeleteStatus
    {
        public DeletedEnum() : base("Deleted", 2) { }

        public override bool IsFailure => false;
        public override bool? IsTransient => null;
    }

    private sealed class TransientFailEnum : UpsertOrDeleteStatus
    {
        public TransientFailEnum() : base("Failed", 3) { }

        public override bool IsFailure => true;
        public override bool? IsTransient => true;
    }

    private sealed class NonTransientFailEnum : UpsertOrDeleteStatus
    {
        public NonTransientFailEnum() : base("Failed", 3) { }

        public override bool IsFailure => true;
        public override bool? IsTransient => false;
    }
}


