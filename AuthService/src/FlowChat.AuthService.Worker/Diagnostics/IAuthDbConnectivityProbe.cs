namespace FlowChat.AuthService.Worker.Diagnostics;

public interface IAuthDbConnectivityProbe
{
    Task ProbeAsync(CancellationToken cancellationToken);
}
