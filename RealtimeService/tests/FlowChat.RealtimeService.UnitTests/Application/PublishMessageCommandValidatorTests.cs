using AutoFixture;
using FlowChat.RealtimeService.Application.Features.Message.Commands.PublishMessage;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Application;

public sealed class PublishMessageCommandValidatorTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly PublishMessageCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsNoValidationErrors()
    {
        var command = new PublishMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "John Doe",
            "Hello",
            42,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyGuids_ReturnsValidationErrors()
    {
        var command = new PublishMessageCommand(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            "John Doe",
            "Hello",
            42,
            DateTimeOffset.UtcNow,
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
    public void Validate_NullOrWhitespaceSenderDisplayName_ReturnsValidationError(string? value)
    {
        var command = new PublishMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            value,
            "Hello",
            42,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SenderDisplayName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhitespaceText_ReturnsValidationError(string? value)
    {
        var command = new PublishMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "John Doe",
            value,
            42,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Text");
    }

    [Fact]
    public void Validate_RecipientUserIdsContainsOnlyEmptyGuids_ReturnsValidationError()
    {
        var command = new PublishMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "John Doe",
            "Hello",
            42,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [Guid.Empty, Guid.Empty]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientUserIds");
    }

    [Fact]
    public void Validate_EmptyRecipientUserIds_ReturnsValidationError()
    {
        var command = new PublishMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "John Doe",
            "Hello",
            42,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RecipientUserIds");
    }

    [Fact]
    public void Validate_NonPositiveSequenceNum_ReturnsValidationError()
    {
        var command = new PublishMessageCommand(
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            _fixture.Create<Guid>(),
            "John Doe",
            "Hello",
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [_fixture.Create<Guid>()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SequenceNum");
    }
}
