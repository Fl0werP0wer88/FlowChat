using System.Security.Claims;
using AutoMapper;
using FlowChat.ChatService.Api.Features.Conversation.Public.GetContactsForUser;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetContactsForUser;
using FlowChat.Core.Results;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FlowChat.ChatService.UnitTests.API.Features.Conversation.Public.GetContactsForUser;

public sealed class GetContactsForUserControllerTests
{
    private static readonly IMapper Mapper =
        new MapperConfiguration(
            cfg => cfg.AddProfile<GetContactsForUserMappingProfile>(),
            Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance).CreateMapper();

    private readonly Mock<IMediator> _mediatorMock = new();

    [Fact]
    public async Task GetContactsForUser_WhenFound_Returns200WithContacts()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var dto = new ContactDto(
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
            .Setup(x => x.Send(It.IsAny<GetContactsForUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<IReadOnlyCollection<ContactDto>>.Success([dto]));

        var controller = CreateController(userId);

        var actionResult = await controller.GetContactsForUser(CancellationToken.None);

        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<GetContactsForUserResponse>().Subject;
        var contact = response.Contacts.Should().ContainSingle().Subject;
        contact.PartnerUserId.Should().Be(partnerUserId);
        contact.DisplayName.Should().Be("Alice");
        contact.Email.Should().Be("alice@example.com");
        contact.IsMuted.Should().BeTrue();
        contact.IsBlocked.Should().BeFalse();
        _mediatorMock.Verify(
            x => x.Send(
                It.Is<GetContactsForUserQuery>(q => q.RequestingUserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetContactsForUser_WhenNotAuthenticated_Returns401()
    {
        var controller = CreateController();

        var actionResult = await controller.GetContactsForUser(CancellationToken.None);

        actionResult.Should().BeOfType<UnauthorizedResult>();
    }

    private GetContactsForUserController CreateController(Guid? authenticatedUserId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (authenticatedUserId.HasValue)
        {
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", authenticatedUserId.Value.ToString("D"))],
                "Test"));
        }

        return new GetContactsForUserController(_mediatorMock.Object, Mapper)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
