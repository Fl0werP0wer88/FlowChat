namespace FlowChat.AuthService.OutboxPublisher.Diagnostics;

public interface IAuthDbConnectivityProbe
{
    Task ProbeAsync(CancellationToken cancellationToken);
}
