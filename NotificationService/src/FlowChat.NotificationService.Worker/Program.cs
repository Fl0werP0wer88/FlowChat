using FlowChat.API.Abstractions;
using FlowChat.NotificationService.Application;
using FlowChat.NotificationService.Infrastructure;
using FlowChat.NotificationService.Persistence;
using FlowChat.NotificationService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddFlowChatOpenTelemetry(typeof(ApplicationServiceRegistration).Assembly);
    builder.Services.AddWorkerApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    builder.Services.AddWorkerKafkaConsumer(builder.Configuration);
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
            .LogCritical(exception, "NotificationService Worker terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in NotificationService Worker: {exception}");
    }

    throw;
}
