using MediatR;

namespace FlowChat.Shared.Application;

public interface IProjectionBulkCommand<TItem> : ICommand<Unit>
    where TItem : notnull
{
    IReadOnlyCollection<TItem> Items { get; }
}
