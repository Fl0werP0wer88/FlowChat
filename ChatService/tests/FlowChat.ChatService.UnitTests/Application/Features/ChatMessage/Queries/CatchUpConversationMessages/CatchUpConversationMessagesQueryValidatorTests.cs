using FlowChat.ChatService.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.CatchUpConversationMessages;

public sealed class CatchUpConversationMessagesQueryValidatorTests
{
    private readonly CatchUpConversationMessagesQueryValidator _validator = new();

    [Theory]
    [InlineData(0L, null)]
    [InlineData(41L, null)]
    [InlineData(41L, 41L)]
    [InlineData(41L, 50L)]
    public void Validate_ValidCatchUpRequest_ReturnsNoErrors(long after, long? through)
    {
        var result = _validator.Validate(Query(after, through));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeAfterSequence_ReturnsError()
    {
        var result = _validator.Validate(Query(-1, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "AfterSequenceNum");
    }

    [Fact]
    public void Validate_ThroughBelowAfter_ReturnsError()
    {
        var result = _validator.Validate(Query(50, 49));

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_LimitOutsideRange_ReturnsError(int limit)
    {
        var result = _validator.Validate(
            new CatchUpConversationMessagesQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                limit,
                0,
                null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Limit");
    }

    private static CatchUpConversationMessagesQuery Query(long after, long? through) =>
        new(Guid.NewGuid(), Guid.NewGuid(), 100, after, through);
}
