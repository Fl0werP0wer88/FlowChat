using FlowChat.AuthService.Api;

var builder = WebApplication.CreateBuilder(args);

var app = builder.ConfigureServices().ConfigurePipeline();

if (app.Environment.IsDevelopment())
{
    await app.ResetDatabaseAsync();
}

app.Run();
