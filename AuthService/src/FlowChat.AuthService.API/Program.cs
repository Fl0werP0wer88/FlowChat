using FlowChat.AuthService.Api;

WebApplication? app = null;

try
{
    var builder = WebApplication.CreateBuilder(args);

    app = builder.ConfigureServices().ConfigurePipeline();

    if (app.Environment.IsDevelopment())
    {
        await app.ResetDatabaseAsync();
    }

    await app.RunAsync();
}
catch (Exception exception)
{
    if (app is not null)
    {
        app.Logger.LogCritical(exception, "AuthService API terminated unexpectedly.");
    }
    else
    {
        Console.Error.WriteLine($"Fatal startup error in AuthService API: {exception}");
    }

    throw;
}
