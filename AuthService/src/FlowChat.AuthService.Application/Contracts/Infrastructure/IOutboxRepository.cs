namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IOutboxRepository<TEvent>
{
    Task EnqueueAsync(TEvent message, CancellationToken cancellationToken);
}
