using FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace FlowChat.AuthService.UnitTests;

public sealed class ChangeAuthEmailCommandValidatorTests
{
    private readonly ChangeAuthEmailCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoValidationErrors()
    {
        var command = new ChangeAuthEmailCommand
        {
            UserId = Guid.NewGuid(),
            EmailAddress = "flower@example.com"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenUserIdIsEmpty_ReturnsValidationError()
    {
        var command = new ChangeAuthEmailCommand
        {
            UserId = Guid.Empty,
            EmailAddress = "flower@example.com"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "UserId is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenEmailAddressIsBlank_ReturnsValidationError(string emailAddress)
    {
        var command = new ChangeAuthEmailCommand
        {
            UserId = Guid.NewGuid(),
            EmailAddress = emailAddress
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Email address is required.");
    }

    [Fact]
    public void Validate_WhenEmailAddressIsInvalid_ReturnsValidationError()
    {
        var command = new ChangeAuthEmailCommand
        {
            UserId = Guid.NewGuid(),
            EmailAddress = "not-an-email"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == EmailAddress.InvalidEmailAddressMessage);
    }
}
