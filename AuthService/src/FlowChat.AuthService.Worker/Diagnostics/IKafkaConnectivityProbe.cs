namespace FlowChat.AuthService.Worker.Diagnostics;

public interface IKafkaConnectivityProbe
{
    Task ProbeAsync(CancellationToken cancellationToken);
}
