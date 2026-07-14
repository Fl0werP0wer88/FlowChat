using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.GetDuetConversation;

public sealed class GetDuetConversationControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<GetDuetConversationMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    private GetDuetConversationController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        return new GetDuetConversationController(_mediatorMock.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    [Fact]
    public async Task GetDuetConversation_WhenFound_Returns200WithResponse()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var dto = new DuetConversationDetailDto(
            Guid.NewGuid(),
            [
                new ConversationParticipantDto(userId, "Me", "me.png", userId),
                new ConversationParticipantDto(partnerUserId, "Partner", "partner.png", partnerUserId)
            ]);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetDuetConversationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<DuetConversationDetailDto>.Success(dto));

        var controller = CreateController(userId);

        var actionResult = await controller.GetDuetConversation(partnerUserId, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetDuetConversationResponse>().Subject;
        response.ConversationId.Should().Be(dto.ConversationId);
        response.Participants.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDuetConversation_WhenNotFound_Returns404()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetDuetConversationQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<DuetConversationDetailDto>.Failure(
                DomainError.NotFound("No duet conversation found between the specified users.")));

        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.GetDuetConversation(Guid.NewGuid(), CancellationToken.None);

        actionResult.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetDuetConversation_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.GetDuetConversation(Guid.NewGuid(), CancellationToken.None);

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
