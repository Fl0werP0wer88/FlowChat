using FlowChat.ChatService.Api;
using FlowChat.ChatService.Infrastructure.Configuration.Settings;
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
            ["Kafka:ConversationV2Producer:BootstrapServers"] = "localhost:9092",
            ["Kafka:ConversationV2Producer:Topic"] = "dev.flowchat.chat.conversation-projection.v2",
            ["Kafka:ConversationMembershipV2ProjectionProducer:BootstrapServers"] = "localhost:9092",
            ["Kafka:ConversationMembershipV2ProjectionProducer:Topic"] = "dev.flowchat.chat.conversation-membership-projection.v2",
            ["Kafka:ConversationParticipantV2Producer:BootstrapServers"] = "localhost:9092",
            ["Kafka:ConversationParticipantV2Producer:Topic"] = "dev.flowchat.chat.conversation-participant-projection.v2",
            ["Kafka:ChatMessageV2Producer:BootstrapServers"] = "localhost:9092",
            ["Kafka:ChatMessageV2Producer:Topic"] = "dev.flowchat.chat.message.v2",
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
        app.Services.GetRequiredService<IOptions<ConversationV2ProducerSettingsSection>>()
            .Value.Topic.Should().Be("dev.flowchat.chat.conversation-projection.v2");
    }
}
