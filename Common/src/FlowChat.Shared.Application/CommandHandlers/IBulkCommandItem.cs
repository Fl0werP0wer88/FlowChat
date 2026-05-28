namespace FlowChat.Shared.Application;

public interface IBulkCommandItem
{
    bool MarkedForDeletion { get; }
}
