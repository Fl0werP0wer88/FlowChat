using System.Text.Json;
using FlowChat.AuthService.OutboxPublisher;
using FlowChat.AuthService.OutboxPublisher.Diagnostics;
using FlowChat.AuthService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace FlowChat.AuthService.UnitTests;

public sealed class OutboxPublisherStartupDiagnosticsTests
{
    [Fact]
    public void AddOutboxPublisher_RegistersOutboxWorkerHostedService()
    {
        var configuration = BuildOutboxPublisherConfiguration();
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));
        builder.Services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")),
            ServiceLifetime.Scoped);
        builder.Services.AddOutboxPublisher(builder.Configuration);

        using var host = builder.Build();
        var hostedServices = host.Services.GetServices<IHostedService>().ToList();

        Assert.Contains(
            hostedServices,
            hostedService => hostedService.GetType().FullName?.Contains("OutboxWorkerService") == true);
    }

    [Fact]
    public async Task OutboxPublisherProgramServices_ResolvesDbContextFactory()
    {
        var configuration = BuildOutboxPublisherConfiguration();
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AuthDb")));
        services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(configuration.GetConnectionString("AuthDb")),
            ServiceLifetime.Scoped);

        await using var serviceProvider = services.BuildServiceProvider();
        var dbContextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();

        Assert.NotNull(dbContextFactory);
    }

    [Theory]
    [InlineData("AuthService/src/Workers/FlowChat.AuthService.OutboxPublisher/appsettings.json")]
    [InlineData("AuthService/src/Workers/FlowChat.AuthService.OutboxPublisher/appsettings.Development.json")]
    public void OutboxPublisherAppSettingsFiles_ExposeLoggingAndOutboxSettings(string relativePath)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(GetRepositoryPath(relativePath))
            .Build();

        var outboxSection = configuration.GetSection("OutboxPublisher");

        Assert.True(outboxSection.Exists());
        Assert.True(outboxSection.GetValue<int>("BatchSize") > 0);
        Assert.True(outboxSection.GetValue<int>("PollIntervalSeconds") > 0);
        Assert.True(outboxSection.GetValue<int>("RetryBaseDelaySeconds") > 0);
        Assert.True(outboxSection.GetValue<int>("MaxRetryDelaySeconds") > 0);
        Assert.Equal("Debug", configuration["Logging:LogLevel:Silverback"]);
        Assert.Equal("Information", configuration["Logging:LogLevel:Microsoft.Hosting.Lifetime"]);
    }

    [Fact]
    public void OutboxPublisherLaunchSettings_SetsDevelopmentEnvironment()
    {
        var launchSettings = File.ReadAllText(
            GetRepositoryPath("AuthService/src/Workers/FlowChat.AuthService.OutboxPublisher/Properties/launchSettings.json"));
        using var jsonDocument = JsonDocument.Parse(launchSettings);

        var environment = jsonDocument.RootElement
            .GetProperty("profiles")
            .GetProperty("FlowChat.AuthService.OutboxPublisher")
            .GetProperty("environmentVariables")
            .GetProperty("DOTNET_ENVIRONMENT")
            .GetString();

        Assert.Equal("Development", environment);
    }

    [Fact]
    public async Task OutboxWorkerStartupProbe_WhenAuthDbProbeFails_ThrowsInvalidOperationException()
    {
        var startupProbe = new OutboxWorkerStartupProbe(
            new ThrowingAuthDbConnectivityProbe(),
            new NoOpKafkaConnectivityProbe(),
            NullLogger<OutboxWorkerStartupProbe>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => startupProbe.StartAsync(CancellationToken.None));

        Assert.Contains("AuthDb connectivity probe failed", exception.Message);
    }

    [Fact]
    public async Task OutboxWorkerStartupProbe_WhenKafkaProbeFails_ThrowsInvalidOperationException()
    {
        var startupProbe = new OutboxWorkerStartupProbe(
            new NoOpAuthDbConnectivityProbe(),
            new ThrowingKafkaConnectivityProbe(),
            NullLogger<OutboxWorkerStartupProbe>.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => startupProbe.StartAsync(CancellationToken.None));

        Assert.Contains("Kafka connectivity probe failed", exception.Message);
    }

    private static IConfiguration BuildOutboxPublisherConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] =
                    "Host=localhost;Port=5432;Database=flowchat_auth_db;Username=flowchat_app;Password=flowchat_app_pw;",
                ["OutboxPublisher:BatchSize"] = "25",
                ["OutboxPublisher:PollIntervalSeconds"] = "3",
                ["OutboxPublisher:RetryBaseDelaySeconds"] = "3",
                ["OutboxPublisher:MaxRetryDelaySeconds"] = "120",
                ["Kafka:AccountRegisteredProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:AccountRegisteredProducer:Topic"] = "dev.flowchat.identity.user.v1"
            })
            .Build();
    }

    private static string GetRepositoryPath(string relativePath)
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDirectory is not null)
        {
            var candidatePath = Path.Combine(currentDirectory.FullName, relativePath);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new InvalidOperationException($"Could not locate file '{relativePath}' starting from '{AppContext.BaseDirectory}'.");
    }

    private sealed class NoOpAuthDbConnectivityProbe : IAuthDbConnectivityProbe
    {
        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingAuthDbConnectivityProbe : IAuthDbConnectivityProbe
    {
        public Task ProbeAsync(CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("db is unavailable"));
    }

    private sealed class NoOpKafkaConnectivityProbe : IKafkaConnectivityProbe
    {
        public Task ProbeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class ThrowingKafkaConnectivityProbe : IKafkaConnectivityProbe
    {
        public Task ProbeAsync(CancellationToken cancellationToken) =>
            Task.FromException(new InvalidOperationException("kafka is unavailable"));
    }
}
