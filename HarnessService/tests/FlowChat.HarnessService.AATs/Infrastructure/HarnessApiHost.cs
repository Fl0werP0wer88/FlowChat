using FlowChat.HarnessService.API;
using FlowChat.HarnessService.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class HarnessApiHost : IAsyncLifetime
{
    private WebApplication? _app;

    private readonly string _connectionString;
    private readonly string _apiKey;

    public HarnessApiHost(string connectionString, string apiKey)
    {
        _connectionString = connectionString;
        _apiKey = apiKey;
    }

    public string BaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(StartupExtensions).Assembly.GetName().Name,
            EnvironmentName = Environments.Development
        });

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["FlowChat:InternalApi:ApiKey"] = _apiKey,
            ["ConnectionStrings:HarnessDb"] = _connectionString
        });

        builder.WebHost.UseUrls("http://127.0.0.1:0");

        _app = builder
            .ConfigureServices()
            .ConfigurePipeline();

        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync();
        }

        await _app.StartAsync();

        var address = _app.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()?
            .Addresses
            .SingleOrDefault();

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new InvalidOperationException("Harness API host started without a bound server address.");
        }

        BaseUrl = address.TrimEnd('/');
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
