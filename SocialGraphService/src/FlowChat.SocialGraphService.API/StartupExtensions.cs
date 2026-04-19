using FlowChat.Shared.API;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.SocialGraphService.Application;
using FlowChat.SocialGraphService.Infrastructure;
using FlowChat.SocialGraphService.Infrastructure.Configuration.Settings;
using FlowChat.SocialGraphService.Infrastructure.Kafka;
using FlowChat.SocialGraphService.Persistence;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Threading.Tasks;

namespace FlowChat.SocialGraphService.Api;

public static class StartupExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var settingsProvider = new AppSettingsProvider(builder.Configuration);
        var jwtSettings = settingsProvider.GetSection<JwtSettingsSection>();
        var jwtKey = jwtSettings.Key;
        var jwtIssuer = jwtSettings.Issuer;
        var jwtAudience = jwtSettings.Audience;

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Key.");
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Audience.");
        }

        builder.Services.AddApiApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddPersistenceServices(builder.Configuration);
        builder.Services.AddApiSilverbackMessaging(builder.Configuration);
        builder.Services.AddAutoMapper(
            (Action<AutoMapper.IMapperConfigurationExpression>?)null,
            typeof(StartupExtensions).Assembly);
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
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.Zero
                };
            });
        builder.Services.AddAuthorization();

        builder.Services.AddControllers();

        builder.Services.AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Wklej sam token JWT (bez prefiksu 'Bearer ')."
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
}

