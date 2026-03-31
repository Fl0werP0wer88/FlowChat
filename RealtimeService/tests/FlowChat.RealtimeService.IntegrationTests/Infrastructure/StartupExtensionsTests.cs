using FlowChat.RealtimeService.Api;
using FlowChat.RealtimeService.Api.Realtime;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class StartupExtensionsTests
{
    [Fact]
    public async Task ConfigureServices_RegistersSignalRAndReadsJwtTokenFromHubQueryString()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "FLOWCHAT_DEVELOPMENT_JWT_KEY_CHANGE_ME_123456789",
            ["JwtSettings:Issuer"] = "FlowChat.AuthService",
            ["JwtSettings:Audience"] = "FlowChat.Client",
            ["FlowChat:InternalApi:ApiKey"] = "internal-key",
            ["RealtimeApi:BaseUrl"] = "http://localhost:5215",
            ["RealtimeApi:ApiKey"] = "worker-key"
        });

        var app = builder.ConfigureServices();
        var hubContext = app.Services.GetRequiredService<IHubContext<ChatHub, IRealtimeClient>>();
        var optionsMonitor = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>();
        var options = optionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Path = "/hubs/chat";
        httpContext.Request.QueryString = new QueryString("?access_token=test-token");
        var authenticationScheme = new AuthenticationScheme(
            JwtBearerDefaults.AuthenticationScheme,
            JwtBearerDefaults.AuthenticationScheme,
            typeof(JwtBearerHandler));
        var messageContext = new MessageReceivedContext(httpContext, authenticationScheme, options);

        await options.Events!.OnMessageReceived(messageContext);

        hubContext.Should().NotBeNull();
        messageContext.Token.Should().Be("test-token");
    }
}
