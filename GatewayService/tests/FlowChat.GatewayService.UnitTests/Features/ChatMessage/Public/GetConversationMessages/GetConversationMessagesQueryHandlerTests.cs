using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Public.GetConversationMessages;
using FlowChat.GatewayService.Api.Services;
using FlowChat.Shared.Domain;
using Moq;

namespace FlowChat.GatewayService.UnitTests.Features.ChatMessage.Public.GetConversationMessages;

public sealed class GetConversationMessagesQueryHandlerTests
{
    private readonly Mock<IChatServiceClient> _client = new();

    [Fact]
    public async Task Handle_BeforeSequenceNum_TranslatesToDescendingInclusiveRange()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var items = new[] { Message(19), Message(18) };
        _client
            .Setup(x => x.GetConversationMessagesRangeDescendingAsync(
                conversationId, userId, 1, 19, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new(items, 1, 19, 30, true)));

        var result = await new GetConversationMessagesQueryHandler(_client.Object).Handle(
            new(conversationId, userId, 2, 20),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NextBeforeSequenceNum.Should().Be(18);
        result.Value.CurrentSequenceNum.Should().Be(30);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DownstreamFailure_PreservesError()
    {
        var error = DomainError.NotFound("conversation");
        _client
            .Setup(x => x.GetConversationMessagesRangeDescendingAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), 1, null, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Failure(error));

        var result = await new GetConversationMessagesQueryHandler(_client.Object).Handle(
            new(Guid.NewGuid(), Guid.NewGuid(), 50, null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    private static ChatMessageClientDto Message(long sequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "text", DateTimeOffset.UtcNow, sequenceNum);
}
