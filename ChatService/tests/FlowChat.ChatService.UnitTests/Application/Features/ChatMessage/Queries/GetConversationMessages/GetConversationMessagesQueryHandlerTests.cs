using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandlerTests
{
    private readonly Mock<IChatMessageReadRepository> _chatMessageReadRepositoryMock = new();
    private readonly Mock<IConversationParticipantReadRepository> _participantReadRepositoryMock = new();
    private readonly GetConversationMessagesQueryHandler _handler;

    public GetConversationMessagesQueryHandlerTests()
    {
        _handler = new GetConversationMessagesQueryHandler(
            _chatMessageReadRepositoryMock.Object,
            _participantReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ConversationNotFound_ReturnsNotFoundFailure()
    {
        var query = new GetConversationMessagesQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            50,
            null,
            null);

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantUserIdsAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid>?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.NotFound);
        result.Error.ErrorMessage.Should().Be("Conversation not found.");
        _chatMessageReadRepositoryMock.Verify(
            x => x.GetPageBeforeAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RequestingUserIsNotParticipant_ReturnsUnauthorizedFailure()
    {
        var requestingUserId = Guid.NewGuid();
        var participantUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var query = new GetConversationMessagesQuery(
            conversationId,
            requestingUserId,
            50,
            null,
            null);

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantUserIdsAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([participantUserId, Guid.NewGuid()]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(ErrorType.Unauthorized);
        result.Error.ErrorMessage.Should().Be("Requesting user is not a participant of this conversation.");
        _chatMessageReadRepositoryMock.Verify(
            x => x.GetPageBeforeAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_RequestingUserIsParticipant_ReturnsMessagePage()
    {
        var requestingUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var beforeSentAtUtc = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        var beforeMessageId = Guid.NewGuid();
        var query = new GetConversationMessagesQuery(
            conversationId,
            requestingUserId,
            25,
            beforeSentAtUtc,
            beforeMessageId);
        var expectedPage = new ConversationMessagesPageDto(
            [
                new ChatMessageDto(
                    Guid.NewGuid(),
                    conversationId,
                    requestingUserId,
                    "Alice",
                    "Hello",
                    beforeSentAtUtc.AddMinutes(-1))
            ],
            null,
            null,
            false);

        _participantReadRepositoryMock
            .Setup(x => x.GetParticipantUserIdsAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([requestingUserId, otherUserId]);

        _chatMessageReadRepositoryMock
            .Setup(x => x.GetPageBeforeAsync(
                query.ConversationId,
                query.Limit,
                query.BeforeSentAtUtc,
                query.BeforeMessageId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedPage);
    }
}
