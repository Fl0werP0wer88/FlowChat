using FlowChat.Shared.API;
using FlowChat.UserProfileService.OutboxPublisher;
using FlowChat.UserProfileService.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

IHost? host = null;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.AddFlowChatOpenTelemetry(typeof(OutboxPublisherServiceRegistration).Assembly);
    builder.Services.AddOutboxPublisher(builder.Configuration);
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("UserProfileDb")));
    builder.Services.AddDbContextFactory<AppDbContext>(
        options => options.UseNpgsql(builder.Configuration.GetConnectionString("UserProfileDb")),
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
            .LogCritical(exception, "UserProfileService OutboxPublisher terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in UserProfileService OutboxPublisher: {exception}");
    }

    throw;
}

