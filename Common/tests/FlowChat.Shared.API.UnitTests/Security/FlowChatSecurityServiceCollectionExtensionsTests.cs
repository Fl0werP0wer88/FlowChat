using FlowChat.Shared.API;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FlowChat.Shared.API.UnitTests.Security;

public sealed class FlowChatSecurityServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFlowChatJwtAuthentication_WhenConfigured_RegistersJwtBearerDefaults()
    {
        var services = new ServiceCollection();
        var configuration = CreateJwtConfiguration();

        services.AddFlowChatJwtAuthentication(configuration);

        using var provider = services.BuildServiceProvider();
        var authenticationOptions = provider.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        authenticationOptions.DefaultAuthenticateScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        authenticationOptions.DefaultChallengeScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        jwtOptions.TokenValidationParameters.ValidIssuer.Should().Be("https://localhost:7236/");
        jwtOptions.TokenValidationParameters.ValidAudience.Should().Be("FlowChat.Client");
        jwtOptions.TokenValidationParameters.ClockSkew.Should().Be(TimeSpan.Zero);
        jwtOptions.TokenValidationParameters.ValidateIssuerSigningKey.Should().BeTrue();
        jwtOptions.TokenValidationParameters.IssuerSigningKey.Should().BeOfType<SymmetricSecurityKey>();
    }

    [Theory]
    [InlineData("JwtSettings:Key", "Missing configuration value: JwtSettingsSection:Key.")]
    [InlineData("JwtSettings:Issuer", "Missing configuration value: JwtSettingsSection:Issuer.")]
    [InlineData("JwtSettings:Audience", "Missing configuration value: JwtSettingsSection:Audience.")]
    public void AddFlowChatJwtAuthentication_WhenRequiredSettingIsMissing_ThrowsInvalidOperationException(
        string missingSetting,
        string expectedMessage)
    {
        var services = new ServiceCollection();
        var settings = CreateJwtSettings();
        settings.Remove(missingSetting);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        var act = () => services.AddFlowChatJwtAuthentication(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage(expectedMessage);
    }

    [Fact]
    public void AddFlowChatJwtAuthentication_WhenConfigureJwtBearerProvided_AppliesAdditionalOptions()
    {
        var services = new ServiceCollection();
        var configuration = CreateJwtConfiguration();

        services.AddFlowChatJwtAuthentication(
            configuration,
            configureJwtBearer: options => options.Events = new JwtBearerEvents());

        using var provider = services.BuildServiceProvider();
        var jwtOptions = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        jwtOptions.Events.Should().NotBeNull();
    }

    [Fact]
    public void AddFlowChatJwtAuthentication_WhenConfigureAuthorizationProvided_AppliesAuthorizationOptions()
    {
        var services = new ServiceCollection();
        var configuration = CreateJwtConfiguration();

        services.AddFlowChatJwtAuthentication(
            configuration,
            configureAuthorization: options => options.AddPolicy("test-policy", policy => policy.RequireAuthenticatedUser()));

        using var provider = services.BuildServiceProvider();
        var authorizationOptions = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        authorizationOptions.GetPolicy("test-policy").Should().NotBeNull();
    }

    [Fact]
    public void AddFlowChatSwaggerWithBearer_WhenCalled_AddsBearerSecurityDefinitionAndRequirement()
    {
        var services = new ServiceCollection();

        services.AddFlowChatSwaggerWithBearer(options =>
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "FlowChat API", Version = "v1" }));

        using var provider = services.BuildServiceProvider();
        var swaggerOptions = provider.GetRequiredService<IOptions<SwaggerGenOptions>>().Value;

        swaggerOptions.SwaggerGeneratorOptions.SwaggerDocs.Should().ContainKey("v1");
        swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes.Should().ContainKey("Bearer");
        swaggerOptions.SwaggerGeneratorOptions.SecuritySchemes["Bearer"].Scheme.Should().Be("bearer");
        swaggerOptions.SwaggerGeneratorOptions.SecurityRequirements.Should().ContainSingle();
    }

    private static IConfiguration CreateJwtConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(CreateJwtSettings())
            .Build();
    }

    private static Dictionary<string, string?> CreateJwtSettings()
    {
        return new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "FLOWCHAT_DEVELOPMENT_JWT_KEY_CHANGE_ME_123456789",
            ["JwtSettings:Issuer"] = "https://localhost:7236/",
            ["JwtSettings:Audience"] = "FlowChat.Client"
        };
    }
}
