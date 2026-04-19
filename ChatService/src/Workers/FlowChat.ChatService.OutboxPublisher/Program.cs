using System.Data.Common;
using FlowChat.Shared.API;
using FlowChat.ChatService.OutboxPublisher;
using FlowChat.ChatService.OutboxPublisher.Configuration.Settings;
using FlowChat.ChatService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.AddFlowChatOpenTelemetry(typeof(OutboxPublisherServiceRegistration).Assembly);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("ChatDb")));
    builder.Services.AddDbContextFactory<AppDbContext>(
        options => options.UseNpgsql(builder.Configuration.GetConnectionString("ChatDb")),
        ServiceLifetime.Scoped);
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
            .LogCritical(exception, "ChatService OutboxPublisher terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in ChatService OutboxPublisher: {exception}");
    }

    throw;
}

static void LogStartupDiagnostics(IHost host)
{
    var logger = host.Services
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("FlowChat.ChatService.OutboxPublisher.Startup");
    var environment = host.Services.GetRequiredService<IHostEnvironment>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();
    var producerOptions = host.Services.GetRequiredService<IOptions<ChatMessageSentProducerSettingsSection>>().Value;
    var outboxOptions = host.Services.GetRequiredService<IOptions<OutboxPublisherRuntimeSettingsSection>>().Value;
    var chatDbTarget = GetChatDbTarget(configuration.GetConnectionString("ChatDb"));

    logger.LogInformation(
        "Starting ChatService outbox publisher in {Environment}. ChatDb target: {Host}:{Port}/{Database}. " +
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

