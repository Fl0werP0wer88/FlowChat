namespace FlowChat.HarnessService.Application.Contracts.Infrastructure;

public interface IConsumerOffsetStore
{
    Task CommitConsumedOffsetsAsync(CancellationToken cancellationToken);
}
