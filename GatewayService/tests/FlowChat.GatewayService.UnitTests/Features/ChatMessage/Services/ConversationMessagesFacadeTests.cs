using FluentAssertions;
using FlowChat.Core.Results;
using FlowChat.GatewayService.Api.Features.ChatMessage.Services;
using FlowChat.GatewayService.Infrastructure.Clients.ChatService;
using FlowChat.Shared.Domain;
using Moq;

namespace FlowChat.GatewayService.UnitTests.Features.ChatMessage.Services;

public sealed class ConversationMessagesFacadeTests
{
    private readonly Mock<IChatServiceClient> _client = new();

    [Fact]
    public async Task GetHistoryAsync_BeforeSequenceNum_TranslatesToDescendingInclusiveRange()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var items = new[] { Message(19), Message(18) };
        _client
            .Setup(x => x.GetConversationMessagesRangeDescendingAsync(
                conversationId, userId, 1, 19, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new(items, 1, 19, 30, true)));

        var result = await Facade().GetHistoryAsync(
            conversationId,
            userId,
            2,
            20,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NextBeforeSequenceNum.Should().Be(18);
        result.Value.CurrentSequenceNum.Should().Be(30);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task GetHistoryAsync_InvalidBeforeSequenceNum_ReturnsBadRequestWithoutCallingClient()
    {
        var result = await Facade().GetHistoryAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            50,
            0,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(BadRequestErrorType());
        _client.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetHistoryAsync_EmptyIdentifier_ReturnsBadRequestWithoutCallingClient(
        bool emptyConversationId)
    {
        var result = await Facade().GetHistoryAsync(
            emptyConversationId ? Guid.Empty : Guid.NewGuid(),
            emptyConversationId ? Guid.NewGuid() : Guid.Empty,
            50,
            null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(BadRequestErrorType());
        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetHistoryAsync_DownstreamFailure_PreservesError()
    {
        var error = DomainError.NotFound("conversation");
        _client
            .Setup(x => x.GetConversationMessagesRangeDescendingAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), 1, null, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Failure(error));

        var result = await Facade().GetHistoryAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            50,
            null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task CatchUpAsync_AfterAndThrough_TranslatesToAscendingInclusiveRange()
    {
        var conversationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var items = new[] { Message(11), Message(12) };
        _client
            .Setup(x => x.GetConversationMessagesRangeAscendingAsync(
                conversationId, userId, 11, 15, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new(items, 11, 15, 20, true)));

        var result = await Facade().CatchUpAsync(
            conversationId,
            userId,
            2,
            10,
            15,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.NextAfterSequenceNum.Should().Be(12);
        result.Value.ThroughSequenceNum.Should().Be(15);
        result.Value.HasMore.Should().BeTrue();
    }

    [Fact]
    public async Task CatchUpAsync_ThroughBelowAfter_ReturnsBadRequestWithoutCallingClient()
    {
        var result = await Facade().CatchUpAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100,
            10,
            9,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(BadRequestErrorType());
        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CatchUpAsync_NegativeAfterSequenceNum_ReturnsBadRequestWithoutCallingClient()
    {
        var result = await Facade().CatchUpAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100,
            -1,
            null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(BadRequestErrorType());
        _client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CatchUpAsync_ExplicitThroughAboveCurrent_ReturnsBadRequest()
    {
        _client
            .Setup(x => x.GetConversationMessagesRangeAscendingAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), 6, 11, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new([], 6, 10, 10, false)));

        var result = await Facade().CatchUpAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100,
            5,
            11,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(BadRequestErrorType());
    }

    [Fact]
    public async Task CatchUpAsync_AfterAboveCurrent_ReturnsBadRequest()
    {
        _client
            .Setup(x => x.GetConversationMessagesRangeAscendingAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), 12, null, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FlowChatResult<ConversationMessagesRangeClientDto>.Success(
                new([], 12, 10, 10, false)));

        var result = await Facade().CatchUpAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100,
            11,
            null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(BadRequestErrorType());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task GetHistoryAsync_InvalidLimit_ReturnsBadRequestWithoutCallingClient(int limit)
    {
        var result = await Facade().GetHistoryAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            limit,
            null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.ErrorType.Should().Be(BadRequestErrorType());
        _client.VerifyNoOtherCalls();
    }

    private ConversationMessagesFacade Facade() => new(_client.Object);

    private static ChatMessageClientDto Message(long sequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "text", DateTimeOffset.UtcNow, sequenceNum);

    private static ErrorType BadRequestErrorType() =>
        DomainError.BadRequest("expected").ErrorType;
}
