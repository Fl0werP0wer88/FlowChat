using System.Security.Claims;
using FlowChat.ChatService.Api.Features.Conversation.Public.CreateGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.CreateGroupConversation;

public sealed class CreateGroupConversationControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private CreateGroupConversationController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        return new CreateGroupConversationController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    [Fact]
    public async Task CreateGroupConversation_WhenConversationWasCreated_Returns201CreatedAndMapsCommand()
    {
        var conversationId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var request = new CreateGroupConversationRequest
        {
            ConversationId = conversationId,
            ParticipantUserIds = [creatorId, memberId],
            Name = "Dev Team"
        };
        CreateGroupConversationCommand? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateGroupConversationCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((cmd, _) => capturedCommand = (CreateGroupConversationCommand)cmd)
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Success(
                new IdempotentCommandResult<Guid>(conversationId, WasAlreadyProcessed: false)));

        var controller = CreateController(creatorId);

        var actionResult = await controller.CreateGroupConversation(request, CancellationToken.None);

        var createdResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<CreateGroupConversationResponse>().Subject;
        response.ConversationId.Should().Be(conversationId);
        capturedCommand!.ConversationId.Should().Be(conversationId);
        capturedCommand.CreatedByUserId.Should().Be(creatorId);
        capturedCommand.Name.Should().Be("Dev Team");
        capturedCommand.ParticipantUserIds.Should().BeEquivalentTo(new[] { creatorId, memberId });
    }

    [Fact]
    public async Task CreateGroupConversation_WhenConversationAlreadyExists_Returns200Ok()
    {
        var conversationId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var request = new CreateGroupConversationRequest
        {
            ConversationId = conversationId,
            ParticipantUserIds = [creatorId, Guid.NewGuid()],
            Name = "Dev Team"
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateGroupConversationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Success(
                new IdempotentCommandResult<Guid>(conversationId, WasAlreadyProcessed: true)));

        var controller = CreateController(creatorId);

        var actionResult = await controller.CreateGroupConversation(request, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<CreateGroupConversationResponse>().Subject;
        response.ConversationId.Should().Be(conversationId);
    }

    [Fact]
    public async Task CreateGroupConversation_CommandFailure_ReturnsProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateGroupConversationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IdempotentCommandResult<Guid>>.Failure(
                DomainError.BadRequest("Name is required.")));

        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.CreateGroupConversation(
            new CreateGroupConversationRequest { ConversationId = Guid.NewGuid(), Name = "", ParticipantUserIds = [] },
            CancellationToken.None);

        var badRequestResult = actionResult.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task CreateGroupConversation_ReturnsUnauthorized_WhenNoClaimPresent()
    {
        var controller = CreateController();

        var actionResult = await controller.CreateGroupConversation(
            new CreateGroupConversationRequest { ConversationId = Guid.NewGuid(), Name = "X", ParticipantUserIds = [] },
            CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
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
