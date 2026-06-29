using AutoFixture;
using FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishConversationChanged;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationChanged;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishConversationChangedControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Publish_WhenApiKeyMatches_DispatchesCommand()
    {
        PublishConversationChangedCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<PublishConversationChangedCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (PublishConversationChangedCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Publish(
            new PublishConversationChangedRequest
            {
                ConversationId = _fixture.Create<Guid>(),
                Type = 2,
                Name = "Dev Team",
                CreatedByUserId = _fixture.Create<Guid>(),
                ParticipantUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Type.Should().Be(2);
    }

    [Fact]
    public async Task Publish_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", new Mock<IMediator>(MockBehavior.Strict));

        var result = await controller.Publish(
            new PublishConversationChangedRequest
            {
                ConversationId = _fixture.Create<Guid>(),
                Type = 2,
                CreatedByUserId = _fixture.Create<Guid>(),
                ParticipantUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    private static PublishConversationChangedController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new PublishConversationChangedController(
            mediatorMock.Object,
            Options.Create(new InternalApiSettingsSection { ApiKey = expectedApiKey }))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        if (providedApiKey is not null)
        {
            controller.HttpContext.Request.Headers["X-Internal-Api-Key"] = providedApiKey;
        }

        return controller;
    }
}
