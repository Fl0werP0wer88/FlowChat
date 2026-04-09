using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;
using FluentAssertions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class InsertUserProfileProjectionCommandValidatorTests
{
    private readonly InsertUserProfileProjectionCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ReturnsValid()
    {
        var result = await _validator.ValidateAsync(CreateValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WhenUserProfileIdIsEmpty_ReturnsValidationError()
    {
        var command = CreateValidCommand() with { UserProfileId = Guid.Empty };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Payload does not contain valid UserProfileId.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenFriendlyUserIdIsBlank_ReturnsValidationError(string friendlyUserId)
    {
        var command = CreateValidCommand() with { FriendlyUserId = friendlyUserId };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Payload does not contain valid FriendlyUserId.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenDisplayNameIsBlank_ReturnsValidationError(string displayName)
    {
        var command = CreateValidCommand() with { DisplayName = displayName };

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.ErrorMessage == "Payload does not contain valid DisplayName.");
    }

    private static InsertUserProfileProjectionCommand CreateValidCommand() =>
        new(
            Guid.NewGuid(),
            "jdoe",
            "John Doe",
            "john@example.com",
            "+48123123123",
            "https://example.com/avatar.jpg",
            "Hello there",
            true,
            DateTimeOffset.UtcNow);
}
