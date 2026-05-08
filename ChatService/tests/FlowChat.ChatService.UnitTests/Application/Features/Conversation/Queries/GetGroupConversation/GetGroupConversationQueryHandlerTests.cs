using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.Conversation.Dtos;
using FlowChat.ChatService.Application.Features.Conversation.Queries.GetGroupConversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Queries.GetGroupConversation;

public sealed class GetGroupConversationQueryHandlerTests
{
    private readonly Mock<IGroupConversationReadRepository> _repositoryMock = new();
    private readonly GetGroupConversationQueryHandler _handler;

    public GetGroupConversationQueryHandlerTests()
    {
        _handler = new GetGroupConversationQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenConversationExists_ReturnsSuccess()
    {
        var conversationId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var query = new GetGroupConversationQuery(conversationId);
        var dto = new GroupConversationDetailDto(
            conversationId,
            "Dev Team",
            [
                new ConversationParticipantDto(userId1, "User1", "u1.png", userId1),
                new ConversationParticipantDto(userId2, "User2", "u2.png", userId2)
            ]);

        _repositoryMock
            .Setup(x => x.GetByIdAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Handle_WhenConversationNotFound_ReturnsNotFoundError()
    {
        var query = new GetGroupConversationQuery(Guid.NewGuid());

        _repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupConversationDetailDto?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
    }
}
