using AutoFixture;
using FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishMessage;
using FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;
using FlowChat.RealtimeService.Infrastructure.Configuration.Settings;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PublishMessageControllerTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task Publish_WhenApiKeyMissing_ReturnsUnauthorized()
    {
        var controller = CreateController("expected-key", new Mock<IMediator>(MockBehavior.Strict));

        var result = await controller.Publish(
            new PublishMessageRequest
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
        PublishMessageCommand? capturedCommand = null;
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(x => x.Send(It.IsAny<PublishMessageCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<FlowChatResult<Unit>>, CancellationToken>((request, _) => capturedCommand = (PublishMessageCommand)request)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController("expected-key", mediatorMock, "expected-key");

        var result = await controller.Publish(
            new PublishMessageRequest
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

    private static PublishMessageController CreateController(
        string expectedApiKey,
        Mock<IMediator> mediatorMock,
        string? providedApiKey = null)
    {
        var controller = new PublishMessageController(
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
