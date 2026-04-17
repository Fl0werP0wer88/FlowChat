using FlowChat.Shared.API;
using FlowChat.AuthService.Application;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Persistence;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.AuthService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var apiSettingsManager = new ApiSettingsManager(builder.Configuration);
        var apiRuntimeSettings = apiSettingsManager.GetApiRuntimeSettingsSection();

        builder.Services
        .AddApplicationServices()
        .AddInfrastructureServices(builder.Configuration)
        .AddAPIPersistenceServices(builder.Configuration)
        .AddApiSilverbackMessaging(builder.Configuration)
        .AddAPIServices(builder.Configuration, builder.Environment);
        builder.Services.AddAutoMapper(
            (Action<AutoMapper.IMapperConfigurationExpression>?)null,
            typeof(StartupExtensions).Assembly);
        builder.AddFlowChatOpenTelemetry(typeof(ApplicationServiceRegistration).Assembly);


        builder.Services.AddControllers();

        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins([apiRuntimeSettings.ApiUrl])
        .AllowAnyMethod()
        .SetIsOriginAllowed(pol => true) // DevNote To be removed whe UI address established
        .AllowAnyHeader()
        .AllowCredentials()));

        builder.Services.AddSwaggerGen();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCors("open");
        app.UseFlowChatGlobalExceptionHandling();
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
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


