using AutoFixture;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishPresenceChange;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;
using FlowChat.Core.Contracts;
using FlowChat.RealtimeService.Infrastructure.Configuration;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishPresenceChangeControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Publish_WhenApiKeyMatches_DispatchesCommand()
    {
        PublishPresenceChangeCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<PublishPresenceChangeCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (PublishPresenceChangeCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Publish(
            new PublishPresenceChangeRequest
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
            new PublishPresenceChangeRequest
            {
                UserId = _fixture.Create<Guid>(),
                Status = PresenceStatus.Active,
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    private static PublishPresenceChangeController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var settingsProviderMock = new Mock<ISettingsProvider>();
        settingsProviderMock
            .Setup(x => x.GetSection<InternalApiSettingsSection>())
            .Returns(new InternalApiSettingsSection { ApiKey = expectedApiKey });

        var controller = new PublishPresenceChangeController(mediatorMock.Object, settingsProviderMock.Object)
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
