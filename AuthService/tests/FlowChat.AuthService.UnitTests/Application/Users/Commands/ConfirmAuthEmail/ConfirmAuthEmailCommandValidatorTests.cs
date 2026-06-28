using FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;
using FlowChat.Shared.Domain.ValueObjects;
using FluentAssertions;

namespace FlowChat.AuthService.UnitTests;

public sealed class ConfirmAuthEmailCommandValidatorTests
{
    private readonly ConfirmAuthEmailCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ReturnsNoValidationErrors()
    {
        var command = new ConfirmAuthEmailCommand
        {
            EmailAddress = "flower@example.com"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenEmailAddressIsBlank_ReturnsValidationError(string emailAddress)
    {
        var command = new ConfirmAuthEmailCommand
        {
            EmailAddress = emailAddress
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Email address is required.");
    }

    [Fact]
    public void Validate_WhenEmailAddressIsInvalid_ReturnsValidationError()
    {
        var command = new ConfirmAuthEmailCommand
        {
            EmailAddress = "not-an-email"
        };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == EmailAddress.InvalidEmailAddressMessage);
    }
}
