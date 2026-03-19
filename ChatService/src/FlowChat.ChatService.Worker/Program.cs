using System.Data.Common;
using FlowChat.API.Abstractions;
using FlowChat.ChatService.Infrastructure.Kafka;
using FlowChat.ChatService.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.AddFlowChatOpenTelemetry();
    builder.Services.AddWorkerPersistenceServices(builder.Configuration);
    builder.Services.AddWorkerSilverbackMessaging(builder.Configuration);

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
            .LogCritical(exception, "ChatService Worker terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in ChatService Worker: {exception}");
    }

    throw;
}

static void LogStartupDiagnostics(IHost host)
{
    var logger = host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FlowChat.ChatService.Worker.Startup");
    var environment = host.Services.GetRequiredService<IHostEnvironment>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();
    var settingsManager = host.Services.GetRequiredService<IWorkerSettingsManager>();
    var producerOptions = settingsManager.GetChatMessageSentProducerOptions();
    var outboxOptions = settingsManager.GetOutboxPublisherRuntimeOptions();
    var chatDbTarget = GetChatDbTarget(configuration.GetConnectionString("ChatDb"));

    logger.LogInformation(
        "Starting ChatService worker in {Environment}. ChatDb target: {Host}:{Port}/{Database}. " +
        "ChatMessageSent Kafka: {BootstrapServers} -> {Topic}. " +
        "Outbox worker settings: BatchSize={BatchSize}, PollIntervalSeconds={PollIntervalSeconds}, " +
        "RetryBaseDelaySeconds={RetryBaseDelaySeconds}, MaxRetryDelaySeconds={MaxRetryDelaySeconds}.",
        environment.EnvironmentName,
        chatDbTarget.Host,
        chatDbTarget.Port,
        chatDbTarget.Database,
        producerOptions.BootstrapServers,
        producerOptions.Topic,
        outboxOptions.BatchSize,
        outboxOptions.PollIntervalSeconds,
        outboxOptions.RetryBaseDelaySeconds,
        outboxOptions.MaxRetryDelaySeconds);
}

static (string Host, string Port, string Database) GetChatDbTarget(string? connectionString)
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
