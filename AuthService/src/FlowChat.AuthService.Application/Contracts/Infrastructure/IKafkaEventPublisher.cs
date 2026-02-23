namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IKafkaEventPublisher<TEvent>
{
    Task PublishAsync(TEvent message, CancellationToken cancellationToken);
}
