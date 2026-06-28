namespace FlowChat.Shared.Application;

public interface IConsumedOffsetCommitter
{
    Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken);
}
