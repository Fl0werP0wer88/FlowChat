using System.Text;
using FlowChat.Shared.API.Configuration.Settings;
using FlowChat.Shared.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FlowChat.Shared.API;

public static class FlowChatSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddFlowChatJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configureJwtBearer = null,
        Action<AuthorizationOptions>? configureAuthorization = null)
    {
        var settingsProvider = new AppSettingsProvider(configuration);
        var jwtSettings = settingsProvider.GetSection<JwtSettingsSection>();

        ValidateJwtSettingsSection(jwtSettings);

        services
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

                configureJwtBearer?.Invoke(options);
            });

        if (configureAuthorization is null)
        {
            services.AddAuthorization();
        }
        else
        {
            services.AddAuthorization(configureAuthorization);
        }

        return services;
    }

    public static IServiceCollection AddFlowChatSwaggerWithBearer(
        this IServiceCollection services,
        Action<SwaggerGenOptions>? configureSwagger = null)
    {
        services.AddSwaggerGen(options =>
        {
            configureSwagger?.Invoke(options);

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

        return services;
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
