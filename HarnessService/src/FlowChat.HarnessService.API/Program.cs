using FlowChat.HarnessService.API;

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
        try
        {
            app.Logger.LogCritical(exception, "HarnessService API terminated unexpectedly.");
        }
        catch
        {
            Console.Error.WriteLine($"HarnessService API terminated unexpectedly: {exception}");
        }
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in HarnessService API: {exception}");
    }

    throw;
}

public partial class Program { }
