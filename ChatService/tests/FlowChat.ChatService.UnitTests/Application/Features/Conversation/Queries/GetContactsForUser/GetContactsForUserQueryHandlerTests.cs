using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetContactsForUser;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Queries.GetContactsForUser;

public sealed class GetContactsForUserQueryHandlerTests
{
    private readonly Mock<IDuetConversationReadRepository> _duetConversationReadRepositoryMock = new();
    private readonly GetContactsForUserQueryHandler _handler;

    public GetContactsForUserQueryHandlerTests()
    {
        _handler = new GetContactsForUserQueryHandler(_duetConversationReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenContactsExist_ReturnsSuccessWithContacts()
    {
        var requestingUserId = Guid.NewGuid();
        var contact = new ContactDto(
            Guid.NewGuid(),
            "Alice",
            null,
            Guid.NewGuid(),
            LastReadMsgSeqNum: 1,
            CurrentMsgSeqNum: 2,
            IsBlocked: false,
            IsBlockedByPartner: false,
            IsMuted: false,
            IsHidden: false);

        _duetConversationReadRepositoryMock
            .Setup(x => x.GetContactsForUserAsync(requestingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([contact]);

        var result = await _handler.Handle(new GetContactsForUserQuery(requestingUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Should().Be(contact);
    }
}
