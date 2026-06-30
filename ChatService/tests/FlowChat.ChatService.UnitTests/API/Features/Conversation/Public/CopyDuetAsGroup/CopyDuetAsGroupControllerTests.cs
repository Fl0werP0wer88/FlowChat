using System.Security.Claims;
using FlowChat.ChatService.Api.Features.Conversation.Public.CopyDuetAsGroup;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.CopyDuetAsGroup;

public sealed class CopyDuetAsGroupControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private CopyDuetAsGroupController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        return new CopyDuetAsGroupController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    [Fact]
    public async Task CopyDuetAsGroup_WhenConversationWasCreated_Returns201()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var dto = new GroupConversationDetailDto(
            conversationId, "Alice/Bob",
            [
                new ConversationParticipantDto(userId, "Alice", null, userId),
                new ConversationParticipantDto(partnerUserId, "Bob", null, partnerUserId)
            ]);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateGroupFromDuetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<GroupConversationDetailDto>.Success(dto));

        var controller = CreateController(userId);
        var request = new CopyDuetAsGroupRequest { NewGroupConversationId = conversationId, PartnerUserId = partnerUserId };

        var actionResult = await controller.CopyDuetAsGroup(request, CancellationToken.None);

        var createdResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<CopyDuetAsGroupResponse>().Subject;
        response.ConversationId.Should().Be(conversationId);
        response.Name.Should().Be("Alice/Bob");
        response.Participants.Should().HaveCount(2);
    }

    [Fact]
    public async Task CopyDuetAsGroup_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.CopyDuetAsGroup(
            new CopyDuetAsGroupRequest { NewGroupConversationId = Guid.NewGuid(), PartnerUserId = Guid.NewGuid() },
            CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CopyDuetAsGroup_WhenDuetNotFound_Returns404()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateGroupFromDuetCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<GroupConversationDetailDto>.Failure(
                DomainError.NotFound("Duet conversation not found.")));

        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.CopyDuetAsGroup(
            new CopyDuetAsGroupRequest { NewGroupConversationId = Guid.NewGuid(), PartnerUserId = Guid.NewGuid() },
            CancellationToken.None);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task CopyDuetAsGroup_SendsCommandWithCurrentUserIdAndPartnerUserId()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        CreateGroupFromDuetCommand? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateGroupFromDuetCommand>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((cmd, _) => capturedCommand = (CreateGroupFromDuetCommand)cmd)
            .ReturnsAsync(FlowChatResult<GroupConversationDetailDto>.Success(
                new GroupConversationDetailDto(conversationId, "Alice/Bob", [])));

        var controller = CreateController(userId);
        await controller.CopyDuetAsGroup(
            new CopyDuetAsGroupRequest { NewGroupConversationId = conversationId, PartnerUserId = partnerUserId },
            CancellationToken.None);

        capturedCommand.Should().NotBeNull();
        capturedCommand!.NewGroupConversationId.Should().Be(conversationId);
        capturedCommand.RequestingUserId.Should().Be(userId);
        capturedCommand.PartnerUserId.Should().Be(partnerUserId);
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
