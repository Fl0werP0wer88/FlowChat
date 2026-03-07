using FlowChat.AuthService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.AuthService.Worker.Diagnostics;

public sealed class AuthDbConnectivityProbe : IAuthDbConnectivityProbe
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AuthDbConnectivityProbe(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task ProbeAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (!await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            throw new InvalidOperationException("Unable to connect to AuthDb.");
        }

        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
    }
}
