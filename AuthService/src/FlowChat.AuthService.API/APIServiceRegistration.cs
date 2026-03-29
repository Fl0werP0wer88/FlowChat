using System.Text;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Persistence.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace FlowChat.AuthService.Persistence;

public static class APIServiceRegistration
{
    public static IServiceCollection AddAPIServices(
                            this IServiceCollection services,
                            IConfiguration configuration)
    {
        var apiSettingsManager = new ApiSettingsManager(configuration);
        var jwtSettings = apiSettingsManager.GetJwtSettings();
        var jwtKey = jwtSettings.Key;
        var jwtIssuer = jwtSettings.Issuer;
        var jwtAudience = jwtSettings.Audience;

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Key.");
        }

        if (string.IsNullOrWhiteSpace(jwtIssuer))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Issuer.");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettings:Audience.");
        }

        services.AddDataProtection();

        services.AddIdentityCore<UserEntity>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredUniqueChars = 3;
        })
            .AddRoles<RoleEntity>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IUserStore<UserEntity>>(sp =>
        {
            var context = sp.GetRequiredService<AppDbContext>();
            var describer = sp.GetRequiredService<IdentityErrorDescriber>();

            return new UserStore<UserEntity, RoleEntity, AppDbContext, Guid>(
                context,
                describer)
            {
                AutoSaveChanges = false
            };
        });

        services.AddAuthentication(options =>
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

        services.AddAuthorization();

        return services;
    }
}
