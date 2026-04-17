using System.Text;
using FlowChat.AuthService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace FlowChat.AuthService.Persistence;

public static class APIServiceRegistration
{
    public static IServiceCollection AddAPIServices(
                            this IServiceCollection services,
                            IConfiguration configuration,
                            IHostEnvironment environment)
    {
        var apiSettingsManager = new ApiSettingsManager(configuration);
        var jwtSettings = apiSettingsManager.GetJwtSettingsSection();
        var jwtKey = jwtSettings.Key;
        var encryptionKeyValue = jwtSettings.EncryptionKey;
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

        if (string.IsNullOrWhiteSpace(encryptionKeyValue))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:EncryptionKey.");
        }

        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            throw new InvalidOperationException("Missing configuration value: JwtSettingsSection:Audience.");
        }

        services.AddDataProtection();

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

        var jwtKeyBytes = Encoding.UTF8.GetBytes(jwtKey);
        var encryptionKeyBytes = Encoding.UTF8.GetBytes(encryptionKeyValue);

        if (encryptionKeyBytes.Length != 32)
        {
            throw new InvalidOperationException(
                $"Invalid configuration value: JwtSettingsSection:EncryptionKey must be 256 bits (32 bytes), received {encryptionKeyBytes.Length * 8} bits.");
        }

        var signingKey = new SymmetricSecurityKey(jwtKeyBytes);
        var encryptionKey = new SymmetricSecurityKey(encryptionKeyBytes);

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AppDbContext>();
            })
            .AddServer(options =>
            {
                options.SetIssuer(new Uri(jwtIssuer, UriKind.Absolute));
                options.SetTokenEndpointUris("/api/users/login", "/api/users/refresh-token");
                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();
                options.AcceptAnonymousClients();

                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(jwtSettings.ExpiresMinutes));
                options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(jwtSettings.RefreshTokenExpiresMinutes));

                options.AddSigningKey(signingKey);
                options.AddEncryptionKey(encryptionKey);

                // OpenIddict requires an asymmetric signing credential for identity tokens.
                if (environment.IsDevelopment())
                {
                    options.AddDevelopmentSigningCertificate();
                }
                else
                {
                    options.AddEphemeralSigningKey();
                }

                options.DisableAccessTokenEncryption();

                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough();
            });

        services.AddAuthorization();

        return services;
    }
}
