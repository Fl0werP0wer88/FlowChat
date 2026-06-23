using FlowChat.RealtimeService.Api;

WebApplication? app = null;

try
{
    var builder = WebApplication.CreateBuilder(args);

    app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    await app.RunAsync();
}
catch (Exception exception)
{
    if (app is not null)
    {
        app.Logger.LogCritical(exception, "RealtimeService API terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in RealtimeService API: {exception}");
    }

    throw;
}
