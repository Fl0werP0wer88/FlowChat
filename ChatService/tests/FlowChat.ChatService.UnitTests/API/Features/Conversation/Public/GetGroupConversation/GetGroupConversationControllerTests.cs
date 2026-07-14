using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversation;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.GetGroupConversation;

public sealed class GetGroupConversationControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<GetGroupConversationMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    private GetGroupConversationController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        return new GetGroupConversationController(_mediatorMock.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    [Fact]
    public async Task GetGroupConversation_WhenFound_Returns200WithResponse()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var dto = new GroupConversationDetailDto(
            conversationId,
            "Dev Team",
            [
                new ConversationParticipantDto(userId, "Creator", "creator.png", userId),
                new ConversationParticipantDto(memberId, "Member", "member.png", memberId)
            ]);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetGroupConversationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<GroupConversationDetailDto>.Success(dto));

        var controller = CreateController(userId);

        var actionResult = await controller.GetGroupConversation(conversationId, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetGroupConversationResponse>().Subject;
        response.ConversationId.Should().Be(conversationId);
        response.Name.Should().Be("Dev Team");
        response.Participants.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGroupConversation_WhenNotFound_Returns404()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetGroupConversationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<GroupConversationDetailDto>.Failure(
                DomainError.NotFound("Group conversation not found.")));

        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.GetGroupConversation(Guid.NewGuid(), CancellationToken.None);

        actionResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetGroupConversation_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.GetGroupConversation(Guid.NewGuid(), CancellationToken.None);

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
