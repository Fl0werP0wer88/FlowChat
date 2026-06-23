using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Application;

public sealed class RegisterRealtimeConnectionCommandValidatorTests
{
    private readonly RegisterRealtimeConnectionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsNoValidationErrors()
    {
        var command = new RegisterRealtimeConnectionCommand(Guid.NewGuid(), "connection-1");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyUserId_ReturnsValidationError()
    {
        var command = new RegisterRealtimeConnectionCommand(Guid.Empty, "connection-1");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhitespaceConnectionId_ReturnsValidationError(string? value)
    {
        var command = new RegisterRealtimeConnectionCommand(Guid.NewGuid(), value);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConnectionId");
    }
}
