using FlowChat.ChatService.Api;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.ChatService.IntegrationTests.API;

public sealed class StartupExtensionsTests
{
    [Fact]
    public void ConfigureServices_RegistersJwtBearerAuthenticationDefaults()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "FLOWCHAT_DEVELOPMENT_JWT_KEY_CHANGE_ME_123456789",
            ["JwtSettings:Issuer"] = "https://localhost:7236/",
            ["JwtSettings:Audience"] = "FlowChat.Client",
            ["Kafka:ChatMessageSentProducer:BootstrapServers"] = "localhost:9092",
            ["Kafka:ChatMessageSentProducer:Topic"] = "dev.flowchat.chat.message.v1",
            ["Kafka:ConversationCreatedProducer:BootstrapServers"] = "localhost:9092",
            ["Kafka:ConversationCreatedProducer:Topic"] = "dev.flowchat.chat.conversation.v1",
            ["ConnectionStrings:ChatDb"] = "Host=localhost;Port=5432;Database=flowchat_chat_test_db;Username=test;Password=test"
        });

        using var app = builder.ConfigureServices();

        var authenticationOptions = app.Services.GetRequiredService<IOptions<AuthenticationOptions>>().Value;
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var jwtBearerOptions = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);

        authenticationOptions.DefaultAuthenticateScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        authenticationOptions.DefaultChallengeScheme.Should().Be(JwtBearerDefaults.AuthenticationScheme);
        jwtBearerOptions.TokenValidationParameters.ValidIssuer.Should().Be("https://localhost:7236/");
        jwtBearerOptions.TokenValidationParameters.ValidAudience.Should().Be("FlowChat.Client");
    }
}
