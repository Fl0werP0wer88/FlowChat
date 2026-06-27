namespace FlowChat.Shared.Application;

public interface IProjectionOffsetStore
{
    Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken);
}
