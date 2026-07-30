using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Interfaces;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;
using FlowChat.GatewayService.Api.Features.Conversation.Services;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.Shared.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlowChat.GatewayService.UnitTests.Features.Conversation.Services;

public sealed class ConversationFacadeTests
{
    private readonly Mock<IChatServiceClient> _chatClient = new();
    private readonly Mock<IConversationMessagesFacade> _messagesFacade = new();

    [Fact]
    public async Task OpenDuetAsync_ConversationAndMessages_ReturnsSequenceContract()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var message = Message(conversationId, 8);
        _chatClient
            .Setup(x => x.GetDuetConversationAsync(partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DuetConversationClientDto(conversationId, []));
        SetupMessages(conversationId, userId, [message], 7, 12, true);

        var result = await Facade().OpenDuetAsync(
            userId,
            partnerUserId,
            null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ConversationId.Should().Be(conversationId);
        result.Value.NextBeforeSequenceNum.Should().Be(7);
        result.Value.CurrentSequenceNum.Should().Be(12);
        result.Value.Messages.Should().ContainSingle().Which.SequenceNum.Should().Be(8);
    }

    [Fact]
    public async Task OpenDuetAsync_KnownConversationMatches_ReusesPrefetchedMessages()
    {
        var userId = Guid.NewGuid();
        var partnerUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        _chatClient
            .Setup(x => x.GetDuetConversationAsync(partnerUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DuetConversationClientDto(conversationId, []));
        SetupMessages(conversationId, userId, [], null, 5, false);

        var result = await Facade().OpenDuetAsync(
            userId,
            partnerUserId,
            conversationId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _messagesFacade.Verify(
            x => x.GetHistoryAsync(
                conversationId,
                userId,
                10,
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OpenGroupAsync_ConversationAndMessages_ReturnsSequenceContract()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var message = Message(conversationId, 20);
        _chatClient
            .Setup(x => x.GetGroupConversationAsync(conversationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GroupConversationClientDto(conversationId, "group", []));
        SetupMessages(conversationId, userId, [message], 19, 25, true);

        var result = await Facade().OpenGroupAsync(
            userId,
            conversationId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("group");
        result.Value.NextBeforeSequenceNum.Should().Be(19);
        result.Value.Messages.Should().ContainSingle().Which.SequenceNum.Should().Be(20);
    }

    [Fact]
    public async Task OpenGroupAsync_MissingConversation_ReturnsNotFound()
    {
        _chatClient
            .Setup(x => x.GetGroupConversationAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((GroupConversationClientDto?)null);

        var result = await Facade().OpenGroupAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(DomainError.NotFound("expected").ErrorType);
    }

    [Fact]
    public async Task OpenDuetAsync_EmptyPartnerId_ReturnsBadRequestWithoutCallingClient()
    {
        var result = await Facade().OpenDuetAsync(
            Guid.NewGuid(),
            Guid.Empty,
            null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(DomainError.BadRequest("expected").ErrorType);
        _chatClient.VerifyNoOtherCalls();
    }

    private ConversationFacade Facade() =>
        new(
            _chatClient.Object,
            _messagesFacade.Object,
            NullLogger<ConversationFacade>.Instance);

    private void SetupMessages(
        Guid conversationId,
        Guid userId,
        IReadOnlyCollection<ChatMessageClientDto> messages,
        long? nextBefore,
        long current,
        bool hasMore) =>
        _messagesFacade
            .Setup(x => x.GetHistoryAsync(
                conversationId,
                userId,
                10,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<GetConversationMessagesResult>.Success(
                new(messages, nextBefore, current, hasMore)));

    private static ChatMessageClientDto Message(Guid conversationId, long sequenceNum) =>
        new(
            Guid.NewGuid(),
            conversationId,
            Guid.NewGuid(),
            "text",
            DateTimeOffset.UtcNow,
            sequenceNum);
}
