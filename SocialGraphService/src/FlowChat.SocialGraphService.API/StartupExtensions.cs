using FlowChat.SocialGraphService.Application;
using FlowChat.SocialGraphService.Infrastructure;
using FlowChat.SocialGraphService.Persistence;
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
        var jwtKey = builder.Configuration["JwtSettings:Key"]
            ?? throw new InvalidOperationException("Missing configuration value: JwtSettings:Key.");
        var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]
            ?? throw new InvalidOperationException("Missing configuration value: JwtSettings:Issuer.");
        var jwtAudience = builder.Configuration["JwtSettings:Audience"]
            ?? throw new InvalidOperationException("Missing configuration value: JwtSettings:Audience.");

        builder.Services.AddApiApplicationServices();
        builder.Services.AddInfrastructureServices(builder.Configuration);
        builder.Services.AddPersistenceServices(builder.Configuration);
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

        builder.Services.AddCors(
            options => options.AddPolicy(
                "open",
                policy => policy.WithOrigins([builder.Configuration["ApiUrl"] ?? "https://localhost:5000",
                    builder.Configuration["BlazorUrl"] ?? "https://localhost:5010"])
        .AllowAnyMethod()
        .SetIsOriginAllowed(pol => true) // DevNote To be removed whe UI address established
        .AllowAnyHeader()
        .AllowCredentials()));

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
        app.UseCors("open");
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

    public static async Task ResetDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        try
        {
            var context = scope.ServiceProvider.GetService<AppDbContext>();

            if (context != null)
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.MigrateAsync();
            }
        }
        catch (Exception)
        {
            // logowanie dodamy pozniej
        }
    }
}
