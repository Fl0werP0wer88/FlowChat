using FlowChat.UserProfileService.Api;

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
            app.Logger.LogCritical(exception, "UserProfileService API terminated unexpectedly.");
        }
        catch
        {
            Console.Error.WriteLine($"UserProfileService API terminated unexpectedly: {exception}");
        }
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in UserProfileService API: {exception}");
    }

    throw;
}

public partial class Program { }
