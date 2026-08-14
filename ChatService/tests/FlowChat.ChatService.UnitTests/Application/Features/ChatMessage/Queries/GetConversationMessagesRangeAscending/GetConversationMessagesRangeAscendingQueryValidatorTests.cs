using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeAscending;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessagesRangeAscending;

public sealed class GetConversationMessagesRangeAscendingQueryValidatorTests
{
    [Theory]
    [InlineData(null, null, 100, true)]
    [InlineData(1L, 0L, 1, true)]
    [InlineData(0L, null, 100, false)]
    [InlineData(null, -1L, 100, false)]
    [InlineData(null, null, 101, false)]
    public void Validate_Range_ReturnsExpectedResult(
        long? start, long? end, int limit, bool expected)
    {
        var validator = new GetConversationMessagesRangeAscendingQueryValidator();
        var query = new GetConversationMessagesRangeAscendingQuery(
            Guid.NewGuid(), Guid.NewGuid(), start, end, limit);
        var result = validator.Validate(query);

        result.IsValid.Should().Be(expected);
    }
}
