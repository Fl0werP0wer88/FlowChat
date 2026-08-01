using System.Data.Common;
using FlowChat.Shared.API;
using FlowChat.AuthService.OutboxPublisher;
using FlowChat.AuthService.OutboxPublisher.Configuration.Settings;
using FlowChat.AuthService.OutboxPublisher.Diagnostics;
using FlowChat.AuthService.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

IHost? host = null;
ILogger? programLogger = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.AddFlowChatOpenTelemetry(typeof(OutboxPublisherServiceRegistration).Assembly);
    builder.Services.AddOutboxPublisherPersistenceServices(builder.Configuration);
    builder.Services.AddSingleton<IAuthDbConnectivityProbe, AuthDbConnectivityProbe>();
    builder.Services.AddSingleton<IKafkaConnectivityProbe, KafkaConnectivityProbe>();
    builder.Services.AddHostedService<OutboxWorkerStartupProbe>();
    builder.Services.AddOutboxPublisher(builder.Configuration);

    host = builder.Build();
    programLogger = host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Program");

    LogStartupDiagnostics(host);

    await host.RunAsync();
}
catch (Exception exception)
{
    if (programLogger is not null)
    {
        programLogger.LogCritical(exception, "AuthService OutboxPublisher terminated unexpectedly.");
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
    var accountRegisteredOptions = host.Services.GetRequiredService<IOptions<AccountRegisteredProducerSettingsSection>>().Value;
    var accountConfirmedOptions = host.Services.GetRequiredService<IOptions<AccountConfirmedProducerSettingsSection>>().Value;
    var phoneNumberConfirmedOptions = host.Services.GetRequiredService<IOptions<PhoneNumberConfirmedProducerSettingsSection>>().Value;
    var retryOutboxOptions = host.Services.GetRequiredService<IOptions<RetryOutboxKafkaSettingsSection>>().Value;
    var outboxOptions = host.Services.GetRequiredService<IOptions<OutboxPublisherRuntimeSettingsSection>>().Value;
    var authDbTarget = GetAuthDbTarget(configuration.GetConnectionString("AuthDb"));

    logger.LogInformation(
        "Starting AuthService outbox publisher in {Environment}. AuthDb target: {Host}:{Port}/{Database}. " +
        "AccountRegistered Kafka: {AccountRegisteredBootstrapServers} -> {AccountRegisteredTopic}. " +
        "AccountConfirmed Kafka: {AccountConfirmedBootstrapServers} -> {AccountConfirmedTopic}. " +
        "PhoneNumberConfirmed Kafka: {PhoneNumberConfirmedBootstrapServers} -> {PhoneNumberConfirmedTopic}. " +
        "Retry outbox Kafka: {RetryOutboxBootstrapServers} -> {RetryOutboxTopics}. " +
        "Outbox worker settings: BatchSize={BatchSize}, PollInterval={PollInterval}, " +
        "RetryBaseDelaySeconds={RetryBaseDelaySeconds}, MaxRetryDelaySeconds={MaxRetryDelaySeconds}.",
        environment.EnvironmentName,
        authDbTarget.Host,
        authDbTarget.Port,
        authDbTarget.Database,
        accountRegisteredOptions.BootstrapServers,
        accountRegisteredOptions.Topic,
        accountConfirmedOptions.BootstrapServers,
        accountConfirmedOptions.Topic,
        phoneNumberConfirmedOptions.BootstrapServers,
        phoneNumberConfirmedOptions.Topic,
        retryOutboxOptions.BootstrapServers,
        string.Join(", ", retryOutboxOptions.Topics),
        outboxOptions.BatchSize,
        outboxOptions.PollInterval,
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

