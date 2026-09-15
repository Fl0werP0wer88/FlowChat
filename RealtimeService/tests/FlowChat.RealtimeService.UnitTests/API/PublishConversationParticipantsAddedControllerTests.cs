using AutoFixture;
using FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishConversationParticipantsAdded;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsAdded;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishConversationParticipantsAddedControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Publish_WhenApiKeyMatches_MapsParticipantStateToCommand()
    {
        const string apiKey = "expected-key";
        var mediator = new CapturingMediator();
        var controller = CreateController(mediator, apiKey);
        var request = new PublishConversationParticipantsAddedRequest
        {
            ConversationId = _fixture.Create<Guid>(),
            ConversationType = 2,
            ParticipantUserIds = [_fixture.Create<Guid>()],
            ParticipantCount = 4,
            MembershipRevision = 7
        };

        var result = await controller.Publish(request, CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        mediator.LastSentRequest.Should().BeEquivalentTo(new PublishConversationParticipantsAddedCommand(
            request.ConversationId,
            request.ConversationType,
            request.ParticipantUserIds,
            request.ParticipantCount,
            request.MembershipRevision));
    }

    private static PublishConversationParticipantsAddedController CreateController(
        CapturingMediator mediator,
        string apiKey)
    {
        var controller = new PublishConversationParticipantsAddedController(
            mediator,
            Options.Create(new InternalApiSettingsSection { ApiKey = apiKey }))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Request.Headers["X-Internal-Api-Key"] = apiKey;
        return controller;
    }
}
