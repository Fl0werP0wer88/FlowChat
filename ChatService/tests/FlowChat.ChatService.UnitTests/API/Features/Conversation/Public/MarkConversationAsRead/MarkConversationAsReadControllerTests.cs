using System.Security.Claims;
using FlowChat.ChatService.Api.Features.Conversation.Public.MarkConversationAsRead;
using FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.MarkConversationAsRead;

public sealed class MarkConversationAsReadControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task MarkConversationAsRead_WhenAuthenticated_SendsCommandAndReturns204NoContent()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        MarkConversationAsReadCommandV2? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<MarkConversationAsReadCommandV2>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((cmd, _) => capturedCommand = (MarkConversationAsReadCommandV2)cmd)
            .ReturnsAsync(FlowChatResult<Unit>.Success(Unit.Value));

        var controller = CreateController(userId);

        var actionResult = await controller.MarkConversationAsRead(
            conversationId,
            new MarkConversationAsReadRequest(12),
            CancellationToken.None);

        actionResult.Should().BeOfType<NoContentResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.ConversationId.Should().Be(conversationId);
        capturedCommand.ParticipantUserId.Should().Be(userId);
        capturedCommand.SequenceNum.Should().Be(12);
    }

    [Fact]
    public async Task MarkConversationAsRead_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.MarkConversationAsRead(
            Guid.NewGuid(),
            new MarkConversationAsReadRequest(12),
            CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
        _mediatorMock.Verify(
            x => x.Send(It.IsAny<MarkConversationAsReadCommandV2>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task MarkConversationAsRead_WhenCommandFails_ReturnsProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<MarkConversationAsReadCommandV2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<Unit>.Failure(DomainError.NotFound("Conversation not found.")));

        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.MarkConversationAsRead(
            Guid.NewGuid(),
            new MarkConversationAsReadRequest(12),
            CancellationToken.None);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeOfType<ProblemDetails>();
    }

    private MarkConversationAsReadController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))],
                "Test"));
        }

        return new MarkConversationAsReadController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    private sealed class TestProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new()
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null) =>
            new(modelStateDictionary)
            {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance
            };
    }
}
