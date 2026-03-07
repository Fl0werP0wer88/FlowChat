using System.Data.Common;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Worker.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWorkerPersistenceServices(builder.Configuration);
builder.Services.AddSingleton<IAuthDbConnectivityProbe, AuthDbConnectivityProbe>();
builder.Services.AddSingleton<IKafkaConnectivityProbe, KafkaConnectivityProbe>();
builder.Services.AddHostedService<OutboxWorkerStartupProbe>();
builder.Services.AddWorkerSilverbackMessaging(builder.Configuration);

var host = builder.Build();

LogStartupDiagnostics(host);

await host.RunAsync();

static void LogStartupDiagnostics(IHost host)
{
    var logger = host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FlowChat.AuthService.Worker.Startup");
    var environment = host.Services.GetRequiredService<IHostEnvironment>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();
    var settingsManager = host.Services.GetRequiredService<IWorkerSettingsManager>();
    var userCreatedOptions = settingsManager.GetUserCreatedProducerOptions();
    var emailVerificationOptions = settingsManager.GetUserEmailVerificationRequestedProducerOptions();
    var outboxOptions = settingsManager.GetOutboxPublisherRuntimeOptions();
    var authDbTarget = GetAuthDbTarget(configuration.GetConnectionString("AuthDb"));

    logger.LogInformation(
        "Starting AuthService worker in {Environment}. AuthDb target: {Host}:{Port}/{Database}. " +
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
