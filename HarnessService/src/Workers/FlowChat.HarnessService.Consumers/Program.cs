using FlowChat.HarnessService.Consumers;
using FlowChat.Shared.API;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddFlowChatOpenTelemetry(typeof(ConsumersServiceRegistration).Assembly);
    builder.Services.AddConsumers(builder.Configuration);

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
            .LogCritical(exception, "HarnessService Consumers terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in HarnessService Consumers: {exception}");
    }

    throw;
}
