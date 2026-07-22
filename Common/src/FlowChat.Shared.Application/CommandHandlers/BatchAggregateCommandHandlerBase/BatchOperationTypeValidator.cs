using FlowChat.Core.Messaging;

namespace FlowChat.Shared.Application.CommandHandlers.BatchAggregateCommandHandlerBase;

internal static class BatchOperationTypeValidator
{
    public static void Validate(BatchOperationType batchOperationType, bool hasChangedMutations)
    {
        if (!Enum.IsDefined(batchOperationType))
        {
            throw new InvalidOperationException($"Unsupported batch operation type: {batchOperationType}.");
        }

        if (batchOperationType == BatchOperationType.Unspecified && hasChangedMutations)
        {
            throw new InvalidOperationException(
                "An unspecified batch operation type cannot be used with changed aggregate mutations.");
        }
    }
}
