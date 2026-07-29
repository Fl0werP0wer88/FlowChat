using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesControllerTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(
        cfg => cfg.AddProfile<CatchUpConversationMessagesMappingProfile>(),
        NullLoggerFactory.Instance).CreateMapper();
    private readonly Mock<IMediator> _mediator = new();

    [Fact]
    public async Task CatchUpConversationMessages_ValidRequest_MapsQueryAndResponse()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        CatchUpConversationMessagesQuery? captured = null;
        var page = new ConversationMessagesCatchUpPageDto(
            [new ChatMessageDto(Guid.NewGuid(), conversationId, userId, "Hello", DateTimeOffset.UtcNow, 41)],
            41,
            50,
            45,
            true);
        _mediator
            .Setup(x => x.Send(It.IsAny<CatchUpConversationMessagesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<object, CancellationToken>((query, _) => captured = (CatchUpConversationMessagesQuery)query)
            .ReturnsAsync(FlowChatResult<ConversationMessagesCatchUpPageDto>.Success(page));
        var controller = CreateController(userId);

        var result = await controller.CatchUpConversationMessages(
            conversationId,
            40,
            45,
            25,
            CancellationToken.None);

        var response = result.Should().BeOfType<OkObjectResult>().Subject.Value
            .Should().BeOfType<CatchUpConversationMessagesResponse>().Subject;
        captured.Should().Be(
            new CatchUpConversationMessagesQuery(conversationId, userId, 25, 40, 45));
        response.NextAfterSequenceNum.Should().Be(41);
        response.CurrentSequenceNum.Should().Be(50);
        response.ThroughSequenceNum.Should().Be(45);
        response.Items.Should().ContainSingle().Which.SequenceNum.Should().Be(41);
    }

    [Fact]
    public async Task CatchUpConversationMessages_QueryFailure_ReturnsProblemDetails()
    {
        _mediator
            .Setup(x => x.Send(It.IsAny<CatchUpConversationMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesCatchUpPageDto>.Failure(
                DomainError.BadRequest("Invalid snapshot.")));
        var controller = CreateController(Guid.NewGuid());

        var result = await controller.CatchUpConversationMessages(
            Guid.NewGuid(),
            10,
            20,
            100,
            CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>().Subject.Value
            .Should().BeOfType<ProblemDetails>();
    }

    [Fact]
    public async Task CatchUpConversationMessages_NoClaim_ReturnsUnauthorized()
    {
        var controller = CreateController();

        var result = await controller.CatchUpConversationMessages(
            Guid.NewGuid(),
            0,
            null,
            100,
            CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    private CatchUpConversationMessagesController CreateController(Guid? userId = null)
    {
        var context = new DefaultHttpContext();
        if (userId.HasValue)
        {
            context.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", userId.Value.ToString())], "Test"));
        }

        return new CatchUpConversationMessagesController(_mediator.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
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
            new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

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
