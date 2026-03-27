using System.Data.Common;
using FlowChat.Shared.API;
using FlowChat.AuthService.OutboxPublisher;
using FlowChat.AuthService.OutboxPublisher.Configuration;
using FlowChat.AuthService.OutboxPublisher.Diagnostics;
using FlowChat.AuthService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.AddFlowChatOpenTelemetry(typeof(OutboxPublisherServiceRegistration).Assembly);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")));
    builder.Services.AddDbContextFactory<AppDbContext>(
        options => options.UseNpgsql(builder.Configuration.GetConnectionString("AuthDb")),
        ServiceLifetime.Scoped);
    builder.Services.AddSingleton<IAuthDbConnectivityProbe, AuthDbConnectivityProbe>();
    builder.Services.AddSingleton<IKafkaConnectivityProbe, KafkaConnectivityProbe>();
    builder.Services.AddHostedService<OutboxWorkerStartupProbe>();
    builder.Services.AddOutboxPublisher(builder.Configuration);

    host = builder.Build();

    LogStartupDiagnostics(host);

    await host.RunAsync();
}
catch (Exception exception)
{
    if (host is not null)
    {
        host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Program")
            .LogCritical(exception, "AuthService OutboxPublisher terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in AuthService OutboxPublisher: {exception}");
    }

    throw;
}

static void LogStartupDiagnostics(IHost host)
{
    var logger = host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FlowChat.AuthService.OutboxPublisher.Startup");
    var environment = host.Services.GetRequiredService<IHostEnvironment>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();
    var userCreatedOptions = host.Services.GetRequiredService<IOptions<UserCreatedProducerOptions>>().Value;
    var emailVerificationOptions = host.Services
        .GetRequiredService<IOptions<UserEmailVerificationRequestedProducerOptions>>()
        .Value;
    var outboxOptions = host.Services.GetRequiredService<IOptions<OutboxPublisherRuntimeOptions>>().Value;
    var authDbTarget = GetAuthDbTarget(configuration.GetConnectionString("AuthDb"));

    logger.LogInformation(
        "Starting AuthService outbox publisher in {Environment}. AuthDb target: {Host}:{Port}/{Database}. " +
        "UserCreated Kafka: {UserCreatedBootstrapServers} -> {UserCreatedTopic}. " +
        "EmailVerification Kafka: {EmailVerificationBootstrapServers} -> {EmailVerificationTopic}. " +
        "Outbox worker settings: BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, " +
        "RetryBaseDelaySeconds={RetryBaseDelaySeconds}, MaxRetryDelaySeconds={MaxRetryDelaySeconds}.",
        environment.EnvironmentName,
        authDbTarget.Host,
        authDbTarget.Port,
        authDbTarget.Database,
        userCreatedOptions.BootstrapServers,
        userCreatedOptions.Topic,
        emailVerificationOptions.BootstrapServers,
        emailVerificationOptions.Topic,
        outboxOptions.BatchSize,
        outboxOptions.PollIntervalSeconds,
        outboxOptions.RetryBaseDelaySeconds,
        outboxOptions.MaxRetryDelaySeconds);
}

static (string Host, string Port, string Database) GetAuthDbTarget(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return ("<missing>", "<missing>", "<missing>");
    }

    var builder = new DbConnectionStringBuilder
    {
        ConnectionString = connectionString
    };

    return (
        GetConnectionStringPart(builder, "Host"),
        GetConnectionStringPart(builder, "Port"),
        GetConnectionStringPart(builder, "Database"));
}

static string GetConnectionStringPart(DbConnectionStringBuilder builder, string key)
{
    return builder.TryGetValue(key, out var value) && value is not null
        ? value.ToString() ?? "<missing>"
        : "<missing>";
}

