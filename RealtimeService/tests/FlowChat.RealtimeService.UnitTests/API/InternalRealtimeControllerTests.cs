using FlowChat.RealtimeService.Api.Controllers;
using FlowChat.RealtimeService.Application.Messages.Commands.PublishMessage;
using FlowChat.RealtimeService.Application.Presence.Commands.PublishPresenceChange;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class InternalRealtimeControllerTests
{
    [Fact]
    public async Task PublishMessage_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.PublishMessage(
            new PublishMessageRequest
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
    public async Task PublishPresenceChange_WhenApiKeyMatches_DispatchesCommand()
    {
        var controller = CreateController("expected-key", out var mediator);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Internal-Api-Key"] = "expected-key";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.PublishPresenceChange(
            new PublishPresenceChangeRequest
            {
                UserId = Guid.NewGuid(),
                Status = "online",
                ChangedAtUtc = new DateTime(2026, 3, 17, 11, 0, 0, DateTimeKind.Utc),
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        Assert.IsType<PublishPresenceChangeCommand>(mediator.LastSentRequest);
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
