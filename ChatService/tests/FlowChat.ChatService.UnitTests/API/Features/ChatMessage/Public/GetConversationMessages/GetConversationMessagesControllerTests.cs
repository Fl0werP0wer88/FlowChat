using System.Security.Claims;
using FlowChat.ChatService.Api.Features.ChatMessage.Public.GetConversationMessages;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.ChatMessage.Public.GetConversationMessages;

public sealed class GetConversationMessagesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private GetConversationMessagesController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))], "Test"));
        }

        return new GetConversationMessagesController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    [Fact]
    public async Task GetConversationMessages_ValidRequest_ReturnsOkResponseAndMapsQuery()
    {
        var conversationId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var beforeSentAtUtc = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        var beforeMessageId = Guid.NewGuid();
        GetConversationMessagesQuery? capturedQuery = null;
        var page = new ConversationMessagesPageDto(
            [
                new ChatMessageDto(
                    Guid.NewGuid(),
                    conversationId,
                    requestingUserId,
                    "Alice",
                    "Hello",
                    beforeSentAtUtc.AddMinutes(-1))
            ],
            beforeSentAtUtc.AddMinutes(-1),
            Guid.NewGuid(),
            true);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetConversationMessagesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((query, _) => capturedQuery = (GetConversationMessagesQuery)query)
            .ReturnsAsync(FlowChatResult<ConversationMessagesPageDto>.Success(page));

        var controller = CreateController(requestingUserId);

        var actionResult = await controller.GetConversationMessages(
            conversationId,
            25,
            beforeSentAtUtc,
            beforeMessageId,
            CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetConversationMessagesResponse>().Subject;
        capturedQuery.Should().Be(new GetConversationMessagesQuery(
            conversationId,
            requestingUserId,
            25,
            beforeSentAtUtc,
            beforeMessageId));
        response.Items.Should().ContainSingle();
        response.HasMore.Should().BeTrue();
        response.NextBeforeSentAtUtc.Should().Be(page.NextBeforeSentAtUtc);
        response.NextBeforeMessageId.Should().Be(page.NextBeforeMessageId);
    }

    [Fact]
    public async Task GetConversationMessages_QueryFailure_ReturnsProblemDetails()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetConversationMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesPageDto>.Failure(
                DomainError.NotFound("Conversation not found.")));

        var controller = CreateController(Guid.NewGuid());

        var actionResult = await controller.GetConversationMessages(
            Guid.NewGuid(),
            50,
            null,
            null,
            CancellationToken.None);

        var notFoundResult = actionResult.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.Value.Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task GetConversationMessages_ReturnsUnauthorized_WhenNoClaimPresent()
    {
        var controller = CreateController();

        var actionResult = await controller.GetConversationMessages(
            Guid.NewGuid(),
            50,
            null,
            null,
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
