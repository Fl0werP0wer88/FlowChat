using FlowChat.Shared.API;
using FlowChat.SocialGraphService.OutboxPublisher;
using FlowChat.SocialGraphService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddFlowChatOpenTelemetry(typeof(OutboxPublisherServiceRegistration).Assembly);
    builder.Services.AddOutboxPublisher(builder.Configuration);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("SocialGraphDb")));
    builder.Services.AddDbContextFactory<AppDbContext>(
        options => options.UseNpgsql(builder.Configuration.GetConnectionString("SocialGraphDb")),
        ServiceLifetime.Scoped);

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
            .LogCritical(exception, "SocialGraphService OutboxPublisher terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in SocialGraphService OutboxPublisher: {exception}");
    }

    throw;
}
