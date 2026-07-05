using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.Conversation.Public.GetGroupConversations;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversations;
using FlowChat.Core.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.GetGroupConversations;

public sealed class GetGroupConversationsControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(cfg => cfg.AddProfile<GetGroupConversationsMappingProfile>(), Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task GetGroupConversations_WhenFound_Returns200WithSequenceFields()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var dto = new GroupConversationSummaryDto(
            conversationId,
            "Dev Team",
            ParticipantCount: 3,
            LastReadMsgSeqNum: 42,
            CurrentMsgSeqNum: 84);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetGroupConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyCollection<GroupConversationSummaryDto>>.Success([dto]));

        var controller = CreateController(userId);

        var actionResult = await controller.GetGroupConversations(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetGroupConversationsResponse>().Subject;
        var conversation = response.GroupConversations.Should().ContainSingle().Subject;
        conversation.ConversationId.Should().Be(conversationId);
        conversation.Name.Should().Be("Dev Team");
        conversation.ParticipantCount.Should().Be(3);
        conversation.LastReadMsgSeqNum.Should().Be(42);
        conversation.CurrentMsgSeqNum.Should().Be(84);
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<GetGroupConversationsQuery>(q => q.ParticipantUserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetGroupConversations_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.GetGroupConversations(CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    private GetGroupConversationsController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))],
                "Test"));
        }

        return new GetGroupConversationsController(_mediatorMock.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
