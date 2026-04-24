using FlowChat.ChatService.Application.Contracts.Persistence;
using FlowChat.ChatService.Application.Features.ChatMessage.Dtos;
using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FlowChat.ChatService.Domain.Entities.Conversation;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FluentAssertions;
using Moq;
using ConversationAggregate = FlowChat.ChatService.Domain.Entities.Conversation.Conversation;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandlerTests
{
    private readonly Mock<IChatMessageReadRepository> _chatMessageReadRepositoryMock = new();
    private readonly Mock<IConversationWriteRepository> _conversationRepositoryMock = new();
    private readonly GetConversationMessagesQueryHandler _handler;

    public GetConversationMessagesQueryHandlerTests()
    {
        _handler = new GetConversationMessagesQueryHandler(
            _chatMessageReadRepositoryMock.Object,
            _conversationRepositoryMock.Object);
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

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ConversationAggregate?)null);

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
        var conversationId = Id<ConversationAggregate>.New();
        var query = new GetConversationMessagesQuery(
            conversationId.Value,
            requestingUserId,
            50,
            null,
            null);
        var conversation = ConversationAggregate.Restore(
            conversationId,
            isGroup: false,
            name: null,
            createdByUserId: participantUserId,
            participants:
            [
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, participantUserId),
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, Guid.NewGuid())
            ]);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

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
        var conversationId = Id<ConversationAggregate>.New();
        var beforeSentAtUtc = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero);
        var beforeMessageId = Guid.NewGuid();
        var query = new GetConversationMessagesQuery(
            conversationId.Value,
            requestingUserId,
            25,
            beforeSentAtUtc,
            beforeMessageId);
        var conversation = ConversationAggregate.Restore(
            conversationId,
            isGroup: false,
            name: null,
            createdByUserId: requestingUserId,
            participants:
            [
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, requestingUserId),
                ParticipantUser.Create(Id<ParticipantUser>.New(), conversationId, otherUserId)
            ]);
        var expectedPage = new ConversationMessagesPageDto(
            [
                new ChatMessageDto(
                    Guid.NewGuid(),
                    conversationId.Value,
                    requestingUserId,
                    "Alice",
                    "Hello",
                    beforeSentAtUtc.AddMinutes(-1))
            ],
            null,
            null,
            false);

        _conversationRepositoryMock
            .Setup(x => x.GetByIdAsync(query.ConversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(conversation);

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
