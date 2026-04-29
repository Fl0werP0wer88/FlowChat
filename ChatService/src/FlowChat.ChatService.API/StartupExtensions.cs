using System.Text;
using FlowChat.Shared.API;
using FlowChat.ChatService.Application;
using FlowChat.ChatService.Infrastructure;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
using FlowChat.ChatService.Infrastructure.Kafka;
using FlowChat.ChatService.Persistence;
using FlowChat.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace FlowChat.ChatService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var settingsProvider = new AppSettingsProvider(builder.Configuration);
        var jwtSettings = settingsProvider.GetSection<JwtSettingsSection>();

        ValidateJwtSettingsSection(jwtSettings);

        builder.Services
            .AddApplicationServices()
            .AddInfrastructureServices(builder.Configuration)
            .AddApiPersistenceServices(builder.Configuration)
            .AddApiSilverbackMessaging(builder.Configuration);
        builder.AddFlowChatOpenTelemetry(typeof(ApplicationServiceRegistration).Assembly);

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        builder.Services.AddAuthorization();

        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Paste the JWT access token without the 'Bearer ' prefix."
                });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer", document, null),
                    new List<string>()
                }
            });
        });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
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

    private static void ValidateJwtSettingsSection(JwtSettingsSection jwtSettings)
    {
        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Key.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Audience.");
        }
    }
}

