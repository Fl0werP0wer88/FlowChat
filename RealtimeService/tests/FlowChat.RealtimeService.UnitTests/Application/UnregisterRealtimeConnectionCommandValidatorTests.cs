using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Application;

public sealed class UnregisterRealtimeConnectionCommandValidatorTests
{
    private readonly UnregisterRealtimeConnectionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsNoValidationErrors()
    {
        var command = new UnregisterRealtimeConnectionCommand("connection-1");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_NullOrWhitespaceConnectionId_ReturnsValidationError(string? value)
    {
        var command = new UnregisterRealtimeConnectionCommand(value);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ConnectionId");
    }
}
