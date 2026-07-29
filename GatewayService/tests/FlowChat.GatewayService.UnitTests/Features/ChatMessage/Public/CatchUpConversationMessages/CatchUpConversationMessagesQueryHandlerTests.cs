using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.CatchUpConversationMessages;
using FlowChat.GatewayService.Api.Services;
using Moq;

namespace FlowChat.GatewayService.UnitTests.Features.ChatMessage.Public.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesQueryHandlerTests
{
    private readonly Mock<IChatServiceClient> _client = new();

    [Fact]
    public async Task Handle_AfterAndThrough_TranslatesToAscendingInclusiveRange()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var items = new[] { Message(11), Message(12) };
        _client
            .Setup(x => x.GetConversationMessagesRangeAscendingAsync(
                conversationId, userId, 11, 15, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new(items, 11, 15, 20, true)));

        var result = await new CatchUpConversationMessagesQueryHandler(_client.Object).Handle(
            new(conversationId, userId, Limit: 2, AfterSequenceNum: 10, ThroughSequenceNum: 15),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NextAfterSequenceNum.Should().Be(12);
        result.Value.ThroughSequenceNum.Should().Be(15);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ExplicitThroughAboveCurrent_ReturnsBadRequest()
    {
        _client
            .Setup(x => x.GetConversationMessagesRangeAscendingAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), 6, 11, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new([], 6, 10, 10, false)));

        var result = await new CatchUpConversationMessagesQueryHandler(_client.Object).Handle(
            new(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Limit: 100,
                AfterSequenceNum: 5,
                ThroughSequenceNum: 11),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(
            FlowChat.Shared.Domain.DomainError.BadRequest("expected").ErrorType);
    }

    private static ChatMessageClientDto Message(long sequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "text", DateTimeOffset.UtcNow, sequenceNum);
}
