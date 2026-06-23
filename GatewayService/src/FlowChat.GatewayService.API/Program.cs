using FlowChat.GatewayService.Api;

WebApplication? app = null;

try
{
    var builder = WebApplication.CreateBuilder(args);

    app = builder.ConfigureServices().ConfigurePipeline();

    await app.RunAsync();
}
catch (Exception exception)
{
    if (app is not null)
    {
        app.Logger.LogCritical(exception, "GatewayService API terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in GatewayService API: {exception}");
    }

    throw;
}
