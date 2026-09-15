using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversations;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversations;
using FlowChat.Core.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.GetDuetConversations;

public sealed class GetDuetConversationsControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(
            cfg => cfg.AddProfile<GetDuetConversationsMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task GetDuetConversations_WhenFound_Returns200WithDuetConversations()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var dto = new DuetConversationListItemDto(
            partnerUserId,
            "Alice",
            "https://avatar/alice.png",
            "alice@example.com",
            conversationId,
            LastReadMsgSeqNum: 10,
            CurrentMsgSeqNum: 20,
            IsBlocked: false,
            IsBlockedByPartner: false,
            IsMuted: true,
            IsHidden: false);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetDuetConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyCollection<DuetConversationListItemDto>>.Success([dto]));

        var controller = CreateController(userId);

        var actionResult = await controller.GetDuetConversations(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetDuetConversationsResponse>().Subject;
        var conversation = response.Conversations.Should().ContainSingle().Subject;
        conversation.PartnerUserId.Should().Be(partnerUserId);
        conversation.DisplayName.Should().Be("Alice");
        conversation.Email.Should().Be("alice@example.com");
        conversation.IsMuted.Should().BeTrue();
        conversation.IsBlocked.Should().BeFalse();
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<GetDuetConversationsQuery>(q => q.RequestingUserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDuetConversations_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.GetDuetConversations(CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    private GetDuetConversationsController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))],
                "Test"));
        }

        return new GetDuetConversationsController(_mediatorMock.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
