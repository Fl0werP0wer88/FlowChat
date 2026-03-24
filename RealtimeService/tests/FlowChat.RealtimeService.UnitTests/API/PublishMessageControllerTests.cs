using FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishMessage;
using FlowChat.RealtimeService.Application.Features.Messages.Commands.PublishMessage;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishMessageControllerTests
{
    [Fact]
    public async Task Publish_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", out _);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.Publish(
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

        Assert.IsType<AcceptedResult>(result);
        Assert.IsType<PublishMessageCommand>(mediator.LastSentRequest);
    }

    private static PublishMessageController CreateController(string apiKey, out CapturingMediator mediator)
    {
        mediator = new CapturingMediator();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FlowChat:InternalApi:ApiKey"] = apiKey
            })
            .Build();

        return new PublishMessageController(mediator, new ApiSettingsManager(configuration));
    }
}
