using FlowChat.PresenceService.OutboxPublisher;
using FlowChat.PresenceService.Persistence;
using FlowChat.Shared.API;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddFlowChatOpenTelemetry(typeof(OutboxPublisherServiceRegistration).Assembly);
    builder.Services.AddOutboxPublisher(builder.Configuration);
    builder.Services.AddOutboxPublisherPersistenceServices(builder.Configuration);

    host = builder.Build();
    await host.RunAsync();
}
catch (Exception exception)
{
    if (host is not null)
    {
        host.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Program")
            .LogCritical(exception, "PresenceService OutboxPublisher terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in PresenceService OutboxPublisher: {exception}");
    }

    throw;
}
