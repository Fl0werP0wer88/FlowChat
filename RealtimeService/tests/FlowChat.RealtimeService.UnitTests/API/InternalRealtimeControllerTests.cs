using FlowChat.RealtimeService.Api.Controllers;
using FlowChat.RealtimeService.Application.Messages.Commands.ReceiveMessage;
using FlowChat.RealtimeService.Application.Presence.Commands.PresenceChanged;
using FlowChat.RealtimeService.Application.Realtime.Contracts;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class InternalRealtimeControllerTests
{
    [Fact]
    public async Task ReceiveMessage_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.ReceiveMessage(
            new ReceiveMessageRequest
            {
                MessageId = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                SenderUserId = Guid.NewGuid(),
                SenderDisplayName = "John Doe",
                Text = "Hello",
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task PresenceChanged_WhenApiKeyMatches_DispatchesCommand()
    {
        var controller = CreateController("expected-key", out var mediator);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[Infrastructure.Services.RealtimeInternalApiClient.ApiKeyHeaderName] = "expected-key";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.PresenceChanged(
            new PresenceChangedRequest
            {
                UserId = Guid.NewGuid(),
                Status = "online",
                ChangedAtUtc = new DateTime(2026, 3, 17, 11, 0, 0, DateTimeKind.Utc),
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.IsType<PresenceChangedCommand>(mediator.LastSentRequest);
    }

    private static InternalRealtimeController CreateController(string apiKey, out CapturingMediator mediator)
    {
        mediator = new CapturingMediator();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = apiKey
            })
            .Build();

        return new InternalRealtimeController(mediator, new ApiSettingsManager(configuration));
    }
}
