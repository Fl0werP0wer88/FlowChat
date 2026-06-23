using FlowChat.PresenceService.API;

WebApplication? app = null;

try
{
    var builder = WebApplication.CreateBuilder(args);

    app = builder
        .ConfigureServices()
        .ConfigurePipeline();

    if (app.Environment.IsDevelopment())
    {
        await app.MigrateDatabaseAsync();
    }

    await app.RunAsync();
}
catch (Exception exception)
{
    if (app is not null)
    {
        try
        {
            app.Logger.LogCritical(exception, "PresenceService API terminated unexpectedly.");
        }
        catch
        {
            Console.Error.WriteLine($"PresenceService API terminated unexpectedly: {exception}");
        }
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in PresenceService API: {exception}");
    }

    throw;
}

public partial class Program { }
