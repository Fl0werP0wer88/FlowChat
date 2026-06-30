using System.Text.Json.Serialization;
using FlowChat.HarnessService.Application;
using FlowChat.HarnessService.API.Configuration;
using FlowChat.HarnessService.Infrastructure;
using FlowChat.HarnessService.Persistence;
using FlowChat.Shared.API;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.HarnessService.API;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddApiApplicationServices();
        builder.Services.AddApiPersistenceServices(builder.Configuration);
        builder.Services.AddApiInfrastructureServices();
        builder.Services.AddApiSettings(builder.Configuration);
        builder.AddFlowChatOpenTelemetry(typeof(ApiApplicationServiceRegistration).Assembly);

        builder.Services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddSwaggerGen();
        builder.Services.AddEndpointsApiExplorer();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseFlowChatGlobalExceptionHandling();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.LogSwaggerEndpointOnStarted();
        }

        app.UseHttpsRedirection();
        app.MapControllers();

        return app;
    }

    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        await using var context = new AppDbContextFactory().CreateDbContext([]);
        await context.Database.MigrateAsync();
    }
}
