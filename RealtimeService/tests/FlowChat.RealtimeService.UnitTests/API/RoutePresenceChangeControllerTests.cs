using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Api.Features.Realtime.Internal.RoutePresenceChange;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RoutePresenceChangeControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Publish_WhenApiKeyMatches_DispatchesCommand()
    {
        RoutePresenceChangeCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<RoutePresenceChangeCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (RoutePresenceChangeCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Publish(
            new RoutePresenceChangeRequest
            {
                UserId = _fixture.Create<Guid>(),
                Status = PresenceStatus.Active,
                ChangedAtUtc = new DateTimeOffset(2026, 3, 17, 11, 0, 0, TimeSpan.Zero),
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        result.Should().BeOfType<AcceptedResult>();
        capturedCommand.Should().NotBeNull();
    }

    [Fact]
    public async Task Publish_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", new Mock<IMediator>(MockBehavior.Strict));

        var result = await controller.Publish(
            new RoutePresenceChangeRequest
            {
                UserId = _fixture.Create<Guid>(),
                Status = PresenceStatus.Active,
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    private static RoutePresenceChangeController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new RoutePresenceChangeController(
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
