using FlowChat.SocialGraphService.Application;
using FlowChat.SocialGraphService.Infrastructure;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
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
            .LogCritical(exception, "SocialGraphService Worker terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in SocialGraphService Worker: {exception}");
    }

    throw;
}
