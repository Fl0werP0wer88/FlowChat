using System.Text.Json;
using FlowChat.AuthService.OutboxPublisher;
using FlowChat.AuthService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FlowChat.AuthService.IntegrationTests;

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

        hostedServices.Should().Contain(
            hostedService => hostedService.GetType().FullName != null
                && hostedService.GetType().FullName!.Contains("OutboxWorkerService"));
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

        dbContextFactory.Should().NotBeNull();
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

        outboxSection.Exists().Should().BeTrue();
        outboxSection.GetValue<int>("BatchSize").Should().Be(500);
        outboxSection.GetValue<TimeSpan>("PollInterval").Should().Be(TimeSpan.FromMilliseconds(500));
        outboxSection.GetValue<int>("RetryBaseDelaySeconds").Should().Be(3);
        outboxSection.GetValue<int>("MaxRetryDelaySeconds").Should().Be(120);
        configuration["Logging:LogLevel:Silverback"].Should().Be("Warning");
        configuration["Logging:LogLevel:Microsoft.Hosting.Lifetime"].Should().Be("Information");
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

        environment.Should().Be("Development");
    }

    private static IConfiguration BuildOutboxPublisherConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:AuthDb"] =
                    "Host=localhost;Port=5432;Database=flowchat_auth_db;Username=flowchat_app;Password=flowchat_app_pw;",
                ["OutboxPublisher:BatchSize"] = "500",
                ["OutboxPublisher:PollInterval"] = "00:00:00.500",
                ["OutboxPublisher:RetryBaseDelaySeconds"] = "3",
                ["OutboxPublisher:MaxRetryDelaySeconds"] = "120",
                ["Kafka:AccountRegisteredProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:AccountRegisteredProducer:Topic"] = "dev.flowchat.identity.user.v1",
                ["Kafka:AccountConfirmedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:AccountConfirmedProducer:Topic"] = "dev.flowchat.identity.user.v1",
                ["Kafka:PhoneNumberConfirmedProducer:BootstrapServers"] = "localhost:9092",
                ["Kafka:PhoneNumberConfirmedProducer:Topic"] = "dev.flowchat.identity.user.v1",
                ["Kafka:RetryOutbox:BootstrapServers"] = "localhost:9092",
                ["Kafka:RetryOutbox:Topics:0"] = "retry-5s",
                ["Kafka:RetryOutbox:Topics:1"] = "retry-20s",
                ["Kafka:RetryOutbox:Topics:2"] = "retry-60s",
                ["Kafka:RetryOutbox:Topics:3"] = "retry-300s",
                ["Kafka:RetryOutbox:Topics:4"] = "dlq"
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
}
