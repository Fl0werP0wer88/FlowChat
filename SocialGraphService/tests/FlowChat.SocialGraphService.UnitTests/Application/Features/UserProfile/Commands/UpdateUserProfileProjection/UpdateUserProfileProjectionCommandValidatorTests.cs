using FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;
using FluentAssertions;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class UpdateUserProfileProjectionCommandValidatorTests
{
    private readonly UpdateUserProfileProjectionCommandValidator _validator = new();

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

    private static UpdateUserProfileProjectionCommand CreateValidCommand() =>
        new(
            Guid.NewGuid(),
            "jane.doe",
            "jane@example.com",
            "+48987654321",
            "https://example.com/jane.jpg",
            "Updated bio",
            false,
            DateTimeOffset.UtcNow);
}
