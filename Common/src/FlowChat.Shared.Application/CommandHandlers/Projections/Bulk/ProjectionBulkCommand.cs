namespace FlowChat.Shared.Application;

public sealed record ProjectionBulkCommand<TItem>(
    IReadOnlyCollection<TItem> Items)
    : IProjectionBulkCommand<TItem>
    where TItem : notnull;
