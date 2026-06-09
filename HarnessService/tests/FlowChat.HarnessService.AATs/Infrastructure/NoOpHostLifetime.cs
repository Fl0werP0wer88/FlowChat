using Microsoft.Extensions.Hosting;

namespace FlowChat.HarnessService.AATs.Infrastructure;

/// <summary>
/// Test-only host lifetime that never reacts to OS signals, preventing ConsoleLifetime from
/// stopping the in-process consumer host in response to signals sent to the test runner.
/// </summary>
internal sealed class NoOpHostLifetime : IHostLifetime
{
    public Task WaitForStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
