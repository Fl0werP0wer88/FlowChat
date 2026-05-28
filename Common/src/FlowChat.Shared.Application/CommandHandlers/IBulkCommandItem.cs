using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Application;

public sealed record BulkCommandItem<TValue>(Id<TValue> EntityId, TValue? Value)
    where TValue : class;
