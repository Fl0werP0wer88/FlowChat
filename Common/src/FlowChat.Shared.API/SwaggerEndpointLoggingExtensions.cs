using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlowChat.Shared.API;

public static class SwaggerEndpointLoggingExtensions
{
    public static WebApplication LogSwaggerEndpointOnStarted(this WebApplication app, string swaggerPath = "/swagger")
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() =>
        {
            var addresses = GetServerAddresses(app)
                .Select(address => BuildSwaggerUrl(address, swaggerPath))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var swaggerUrl in addresses)
            {
                app.Logger.LogInformation("Swagger UI: {SwaggerUrl}", swaggerUrl);
            }
        });

        return app;
    }

    private static IEnumerable<string> GetServerAddresses(WebApplication app)
    {
        var server = app.Services.GetService<IServer>();
        var addressesFeature = server?.Features.Get<IServerAddressesFeature>();

        return addressesFeature?.Addresses.Count > 0
            ? addressesFeature.Addresses
            : app.Urls;
    }

    private static string BuildSwaggerUrl(string serverAddress, string swaggerPath)
    {
        var normalizedAddress = serverAddress
            .Replace("://0.0.0.0", "://localhost", StringComparison.OrdinalIgnoreCase)
            .Replace("://[::]", "://localhost", StringComparison.OrdinalIgnoreCase)
            .Replace("://+", "://localhost", StringComparison.OrdinalIgnoreCase)
            .Replace("://*", "://localhost", StringComparison.OrdinalIgnoreCase)
            .TrimEnd('/');

        return $"{normalizedAddress}/{swaggerPath.TrimStart('/')}";
    }
}
