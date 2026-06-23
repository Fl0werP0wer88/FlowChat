using FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;
using FluentAssertions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class AddContactCommandValidatorTests
{
    private readonly AddContactCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WhenUserIdIsProvided_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new AddContactCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenFriendlyUserIdIsProvided_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new AddContactCommand(Guid.NewGuid(), Guid.NewGuid(), null, "jdoe", null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenEmailIsProvided_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(new AddContactCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, "john@example.com"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenOwnerUserIdIsEmpty_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(new AddContactCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Payload does not contain valid OwnerUserId.");
    }

    [Fact]
    public async Task Validate_WhenEmailIsInvalid_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(new AddContactCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, "not-an-email"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Payload does not contain valid Email.");
    }

    [Fact]
    public async Task Validate_WhenNoIdentifierIsProvided_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(new AddContactCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Payload must contain exactly one of: UserId, FriendlyUserId or Email.");
    }

    [Fact]
    public async Task Validate_WhenMoreThanOneIdentifierIsProvided_ReturnsValidationError()
    {
        var result = await _validator.ValidateAsync(
            new AddContactCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "jdoe", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Payload must contain exactly one of: UserId, FriendlyUserId or Email.");
    }
}
