namespace FlowChat.AuthService.OutboxPublisher.Diagnostics;

public interface IKafkaConnectivityProbe
{
    Task ProbeAsync(CancellationToken cancellationToken);
}
