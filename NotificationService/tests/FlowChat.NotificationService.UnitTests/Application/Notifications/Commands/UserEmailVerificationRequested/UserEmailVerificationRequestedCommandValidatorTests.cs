using FlowChat.NotificationService.Application.Features.Notifications.Commands.UserEmailVerificationRequested;
using FluentAssertions;

namespace FlowChat.NotificationService.UnitTests.Application.Notifications.Commands.UserEmailVerificationRequested;

public sealed class UserEmailVerificationRequestedCommandValidatorTests
{
    private readonly UserEmailVerificationRequestedCommandValidator _validator = new();

    private static UserEmailVerificationRequestedCommand ValidCommand() =>
        new(
            Guid.NewGuid(),
            "user@example.com",
            "john.doe",
            "John Doe",
            "https://example.com/confirm?token=abc",
            "source-key-1");

    [Fact]
    public async Task Validate_WithValidCommand_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenUserIdIsEmpty_ReturnsValidationError()
    {
        var command = ValidCommand() with { UserId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "UserId is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenEmailIsBlank_ReturnsValidationError(string email)
    {
        var command = ValidCommand() with { Email = email };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Email is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenUserNameIsBlank_ReturnsValidationError(string userName)
    {
        var command = ValidCommand() with { UserName = userName };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "UserName is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenConfirmationLinkIsBlank_ReturnsValidationError(string link)
    {
        var command = ValidCommand() with { ConfirmationLink = link };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "ConfirmationLink is required.");
    }

    [Fact]
    public async Task Validate_WithNullSourceMessageKey_ReturnsValid()
    {
        var command = ValidCommand() with { SourceMessageKey = null };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithMultipleInvalidFields_ReturnsMultipleErrors()
    {
        var command = new UserEmailVerificationRequestedCommand(
            Guid.Empty,
            "",
            "",
            "John",
            "",
            null);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(3);
    }
}
