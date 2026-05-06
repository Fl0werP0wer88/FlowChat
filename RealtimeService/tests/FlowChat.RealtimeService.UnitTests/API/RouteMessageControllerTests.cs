using AutoFixture;
using FlowChat.Core.Contracts;
using FlowChat.RealtimeService.Api.Features.Realtime.Internal.RouteMessage;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteMessageControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Publish_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", new Mock<IMediator>(MockBehavior.Strict));

        var result = await controller.Publish(
            new RouteMessageRequest
            {
                MessageId = _fixture.Create<Guid>(),
                ConversationId = _fixture.Create<Guid>(),
                SenderUserId = _fixture.Create<Guid>(),
                SenderDisplayName = "John Doe",
                Text = "Hello",
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Publish_WhenApiKeyMatches_DispatchesCommand()
    {
        RouteMessageCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<RouteMessageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (RouteMessageCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Publish(
            new RouteMessageRequest
            {
                MessageId = _fixture.Create<Guid>(),
                ConversationId = _fixture.Create<Guid>(),
                SenderUserId = _fixture.Create<Guid>(),
                SenderDisplayName = "John Doe",
                Text = "Hello",
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
    }

    private static RouteMessageController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<InternalApiSettingsSection>())
            .Returns(new InternalApiSettingsSection { ApiKey = expectedApiKey });

        var controller = new RouteMessageController(mediatorMock.Object, settingsProviderMock.Object)
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
