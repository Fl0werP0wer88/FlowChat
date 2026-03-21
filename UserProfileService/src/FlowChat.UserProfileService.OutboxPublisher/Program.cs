using FlowChat.API.Abstractions;
using FlowChat.UserProfileService.OutboxPublisher;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddFlowChatOpenTelemetry(typeof(OutboxPublisherServiceRegistration).Assembly);
    builder.Services.AddOutboxPublisher(builder.Configuration);
    builder.Services.AddPersistenceServices(builder.Configuration);

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
            .LogCritical(exception, "UserProfileService OutboxPublisher terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in UserProfileService OutboxPublisher: {exception}");
    }

    throw;
}
