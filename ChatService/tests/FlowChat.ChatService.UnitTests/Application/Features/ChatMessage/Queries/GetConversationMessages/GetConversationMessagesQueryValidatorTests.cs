using FlowChat.ChatService.Application.Features.ChatMessage.Queries.GetConversationMessages;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.ChatMessage.Queries.GetConversationMessages;

public sealed class GetConversationMessagesQueryValidatorTests
{
    private readonly GetConversationMessagesQueryValidator _validator = new();

    [Fact]
    public void Validate_ValidQuery_ReturnsNoValidationErrors()
    {
        var query = new GetConversationMessagesQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            50,
            new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero),
            Guid.NewGuid());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyIds_ReturnsValidationErrors()
    {
        var query = new GetConversationMessagesQuery(
            Guid.Empty,
            Guid.Empty,
            50,
            null,
            null);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName)
            .Should().Contain(["ConversationId", "RequestingUserId"]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_LimitOutsideAllowedRange_ReturnsValidationError(int limit)
    {
        var query = new GetConversationMessagesQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            limit,
            null,
            null);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Limit");
    }

    [Fact]
    public void Validate_BeforeMessageIdWithoutBeforeSentAtUtc_ReturnsValidationError()
    {
        var query = new GetConversationMessagesQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            50,
            null,
            Guid.NewGuid());

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "BeforeSentAtUtc");
    }

    [Fact]
    public void Validate_NonUtcBeforeSentAtUtc_ReturnsValidationError()
    {
        var query = new GetConversationMessagesQuery(
            Guid.NewGuid(),
            Guid.NewGuid(),
            50,
            new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.FromHours(2)),
            null);

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "BeforeSentAtUtc");
    }
}
