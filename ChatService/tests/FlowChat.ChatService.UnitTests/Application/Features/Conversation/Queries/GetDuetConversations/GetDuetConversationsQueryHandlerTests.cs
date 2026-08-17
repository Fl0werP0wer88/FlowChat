using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetDuetConversations;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Queries.GetDuetConversations;

public sealed class GetDuetConversationsQueryHandlerTests
{
    private readonly Mock<IDuetConversationReadRepository> _duetConversationReadRepositoryMock = new();
    private readonly GetDuetConversationsQueryHandler _handler;

    public GetDuetConversationsQueryHandlerTests()
    {
        _handler = new GetDuetConversationsQueryHandler(_duetConversationReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenDuetConversationsExist_ReturnsSuccessWithDuetConversations()
    {
        var requestingUserId = Guid.NewGuid();
        var conversation = new DuetConversationListItemDto(
            Guid.NewGuid(),
            "Alice",
            null,
            null,
            Guid.NewGuid(),
            LastReadMsgSeqNum: 1,
            CurrentMsgSeqNum: 2,
            IsBlocked: false,
            IsBlockedByPartner: false,
            IsMuted: false,
            IsHidden: false);

        _duetConversationReadRepositoryMock
            .Setup(x => x.GetDuetConversationsAsync(requestingUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([conversation]);

        var result = await _handler.Handle(new GetDuetConversationsQuery(requestingUserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Should().Be(conversation);
    }
}
