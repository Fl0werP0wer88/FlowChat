using FlowChat.Shared.API;
using FlowChat.AuthService.Application;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
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
        builder.Services
        .AddApiApplicationServices()
        .AddApiInfrastructureServices(builder.Configuration)
        .AddApiPersistenceServices(builder.Configuration)
        .AddApiSilverbackMessaging(builder.Configuration)
        .AddApiServices(builder.Configuration, builder.Environment);
        builder.Services.AddAutoMapper(
            (Action<AutoMapper.IMapperConfigurationExpression>?)null,
            typeof(StartupExtensions).Assembly);
        builder.AddFlowChatOpenTelemetry(typeof(ApiApplicationServiceRegistration).Assembly);


        builder.Services.AddControllers();

        builder.Services.AddSwaggerGen();

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


