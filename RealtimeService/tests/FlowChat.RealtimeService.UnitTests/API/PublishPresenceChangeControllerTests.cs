using FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishPresenceChange;
using FlowChat.RealtimeService.Application.Presence.Commands.PublishPresenceChange;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishPresenceChangeControllerTests
{
    [Fact]
    public async Task Publish_WhenApiKeyMatches_DispatchesCommand()
    {
        var controller = CreateController("expected-key", out var mediator);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Internal-Api-Key"] = "expected-key";
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.Publish(
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

    [Fact]
    public async Task Publish_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Publish(
            new PublishPresenceChangeRequest
            {
                UserId = Guid.NewGuid(),
                Status = "online",
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static PublishPresenceChangeController CreateController(string apiKey, out CapturingMediator mediator)
    {
        mediator = new CapturingMediator();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = apiKey
            })
            .Build();

        return new PublishPresenceChangeController(mediator, new ApiSettingsManager(configuration));
    }
}
