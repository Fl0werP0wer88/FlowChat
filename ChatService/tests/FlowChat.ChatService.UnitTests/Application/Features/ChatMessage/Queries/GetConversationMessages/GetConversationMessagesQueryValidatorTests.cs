using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryValidatorTests
{
    private readonly GetConversationMessagesQueryValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData(1L)]
    [InlineData(42L)]
    public void Validate_ValidHistoryRequest_ReturnsNoErrors(long? beforeSequenceNum)
    {
        var result = _validator.Validate(Query(beforeSequenceNum));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BeforeSequenceBelowOne_ReturnsError()
    {
        var result = _validator.Validate(Query(0));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "BeforeSequenceNum");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_LimitOutsideRange_ReturnsError(int limit)
    {
        var result = _validator.Validate(
            new GetConversationMessagesQuery(Guid.NewGuid(), Guid.NewGuid(), limit, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Limit");
    }

    private static GetConversationMessagesQuery Query(long? beforeSequenceNum) =>
        new(Guid.NewGuid(), Guid.NewGuid(), 50, beforeSequenceNum);
}
