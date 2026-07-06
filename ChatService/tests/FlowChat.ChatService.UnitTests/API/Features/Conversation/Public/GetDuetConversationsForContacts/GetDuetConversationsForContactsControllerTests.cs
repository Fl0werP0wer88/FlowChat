using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.Conversation.Public.GetDuetConversationsForContacts;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversationsForContacts;
using FlowChat.Core.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.GetDuetConversationsForContacts;

public sealed class GetDuetConversationsForContactsControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(
            cfg => cfg.AddProfile<GetDuetConversationsForContactsMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task GetDuetConversationsForContacts_WhenFound_Returns200WithSequenceFields()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var request = new GetDuetConversationsForContactsRequest([partnerUserId]);
        var dto = new DuetConversationForContactDto(
            partnerUserId,
            conversationId,
            LastReadMsgSeqNum: 42,
            CurrentMsgSeqNum: 84);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetDuetConversationsForContactsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyCollection<DuetConversationForContactDto>>.Success([dto]));

        var controller = CreateController(userId);

        var actionResult = await controller.GetDuetConversationsForContacts(request, CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetDuetConversationsForContactsResponse>().Subject;
        var conversation = response.Conversations.Should().ContainSingle().Subject;
        conversation.PartnerUserId.Should().Be(partnerUserId);
        conversation.ConversationId.Should().Be(conversationId);
        conversation.LastReadMsgSeqNum.Should().Be(42);
        conversation.CurrentMsgSeqNum.Should().Be(84);
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<GetDuetConversationsForContactsQuery>(q =>
                    q.RequestingUserId == userId &&
                    q.PartnerUserIds.SequenceEqual(request.PartnerUserIds)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDuetConversationsForContacts_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.GetDuetConversationsForContacts(
            new GetDuetConversationsForContactsRequest([Guid.NewGuid()]),
            CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    private GetDuetConversationsForContactsController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))],
                "Test"));
        }

        return new GetDuetConversationsForContactsController(_mediatorMock.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
