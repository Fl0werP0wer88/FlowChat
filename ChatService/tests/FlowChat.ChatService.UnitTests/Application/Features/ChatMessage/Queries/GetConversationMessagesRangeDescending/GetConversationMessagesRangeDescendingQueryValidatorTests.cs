using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeDescending;

public sealed class GetConversationMessagesRangeDescendingQueryValidatorTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(1L, 10L, true)]
    [InlineData(0L, null, false)]
    [InlineData(null, -1L, false)]
    public void Validate_Range_ReturnsExpectedResult(long? start, long? end, bool expected)
    {
        var validator = new GetConversationMessagesRangeDescendingQueryValidator();
        var query = new GetConversationMessagesRangeDescendingQuery(
            Guid.NewGuid(), Guid.NewGuid(), start, end, 50);
        var result = validator.Validate(query);

        result.IsValid.Should().Be(expected);
    }
}
