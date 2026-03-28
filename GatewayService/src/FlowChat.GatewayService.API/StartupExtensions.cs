using System.Text;
using FlowChat.GatewayService.Api.Configuration;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace FlowChat.GatewayService.Api;

public static class StartupExtensions
{
    private const string ClientCorsPolicyName = "gateway-client";
    private const string AuthenticatedUserPolicyName = "GatewayAuthenticated";

    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.AddFlowChatOpenTelemetry();

        var jwtSettings = builder.Configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>()
            ?? throw new InvalidOperationException("Missing configuration section: JwtSettings.");

        var clientSettings = builder.Configuration
            .GetSection(GatewayClientSettings.SectionName)
            .Get<GatewayClientSettings>()
            ?? new GatewayClientSettings();

        ValidateJwtSettings(jwtSettings);

        builder.Services.Configure<GatewayCatalogOptions>(
            builder.Configuration.GetSection(GatewayCatalogOptions.SectionName));

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
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/chat"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthenticatedUserPolicyName,
                policy => policy.RequireAuthenticatedUser());

            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(
                ClientCorsPolicyName,
                policy =>
                {
                    if (clientSettings.AllowedOrigins.Count > 0)
                    {
                        policy.WithOrigins(clientSettings.AllowedOrigins.ToArray());
                    }

                    policy.AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "FlowChat GatewayService API",
                    Version = "v1",
                    Description = "Direct endpoints exposed by the FlowChat gateway."
                });

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

        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCors(ClientCorsPolicyName);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "FlowChat GatewayService API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseWebSockets();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapReverseProxy();

        return app;
    }

    private static void ValidateJwtSettings(JwtSettings jwtSettings)
    {
        if (string.IsNullOrWhiteSpace(jwtSettings.Key))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Key.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Audience.");
        }
    }
}
