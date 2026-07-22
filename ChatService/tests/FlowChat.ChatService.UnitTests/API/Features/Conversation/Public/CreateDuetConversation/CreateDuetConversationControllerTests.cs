using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.Conversation.Public.CreateDuetConversation;
using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;
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

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.CreateDuetConversation;

public sealed class CreateDuetConversationControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<CreateDuetConversationMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    private CreateDuetConversationController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        return new CreateDuetConversationController(_mediatorMock.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    [Fact]
    public async Task CreateDuetConversation_WhenConversationWasCreated_ReturnsCreatedResponseAndMapsCommand()
    {
        var request = new CreateDuetConversationRequest
        {
            PartnerUserId = Guid.NewGuid()
        };
        var requestingUserId = Guid.NewGuid();
        var conversation = CreateConversationDetail(Guid.NewGuid(), requestingUserId, request.PartnerUserId);
        CreateDuetConversationCommandV2? capturedCommand = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateDuetConversationCommandV2>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((command, _) => capturedCommand = (CreateDuetConversationCommandV2)command)
            .ReturnsAsync(FlowChatResult<DuetConversationDetailDto>.Success(conversation));

        var controller = CreateController(requestingUserId);

        var actionResult = await controller.CreateDuetConversation(request, CancellationToken.None);

        var createdResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        var response = createdResult.Value.Should().BeOfType<CreateDuetConversationResponse>().Subject;
        capturedCommand.Should().Be(new CreateDuetConversationCommandV2(requestingUserId, request.PartnerUserId));
        response.ConversationId.Should().Be(conversation.ConversationId);
        response.Participants.Select(x => x.UserId).Should().Equal(requestingUserId, request.PartnerUserId);
    }

    [Fact]
    public async Task CreateDuetConversation_CommandFailure_ReturnsProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateDuetConversationCommandV2>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<DuetConversationDetailDto>.Failure(
                DomainError.NotFound("Conversation not found.")));

        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.CreateDuetConversation(
            new CreateDuetConversationRequest { PartnerUserId = Guid.NewGuid() },
            CancellationToken.None);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task CreateDuetConversation_ReturnsUnauthorized_WhenNoClaimPresent()
    {
        var controller = CreateController();

        var actionResult = await controller.CreateDuetConversation(
            new CreateDuetConversationRequest { PartnerUserId = Guid.NewGuid() },
            CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    private static DuetConversationDetailDto CreateConversationDetail(
        Guid conversationId,
        Guid requestingUserId,
        Guid partnerUserId) =>
        new(
            conversationId,
            [
                new ConversationParticipantDto(requestingUserId, "Requester", "requester.png", requestingUserId),
                new ConversationParticipantDto(partnerUserId, "Partner", "partner.png", partnerUserId)
            ]);

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
