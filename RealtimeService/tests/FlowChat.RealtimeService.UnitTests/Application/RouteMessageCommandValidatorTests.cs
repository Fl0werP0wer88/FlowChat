using AutoFixture;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Application;

public sealed class RouteMessageCommandValidatorTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly RouteMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsNoValidationErrors()
    {
        var command = new RouteMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "Hello",
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyGuids_ReturnsValidationErrors()
    {
        var command = new RouteMessageCommand(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            "Hello",
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName)
            .Should().Contain(["MessageId", "ConversationId", "SenderUserId"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhitespaceText_ReturnsValidationError(string? value)
    {
        var command = new RouteMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            value,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Text");
    }

    [Fact]
    public void Validate_RecipientUserIdsContainsOnlyEmptyGuids_ReturnsValidationError()
    {
        var command = new RouteMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "Hello",
            DateTimeOffset.UtcNow,
            [Guid.Empty, Guid.Empty]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientUserIds");
    }

    [Fact]
    public void Validate_EmptyRecipientUserIds_ReturnsValidationError()
    {
        var command = new RouteMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "Hello",
            DateTimeOffset.UtcNow,
            []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientUserIds");
    }
}
