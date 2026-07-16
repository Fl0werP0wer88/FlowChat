using AutoFixture;
using FlowChat.Core.Messaging;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationMembershipDelta;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RouteGroupConversationMembershipDeltaCommandValidatorTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly RouteGroupConversationMembershipDeltaCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_ReturnsNoErrors()
    {
        var command = new RouteGroupConversationMembershipDeltaCommand(
            _fixture.Create<Guid>(),
            [_fixture.Create<Guid>()],
            DeltaOperationType.Added,
            1);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidPayload_ReturnsErrorsForAllInvalidFields()
    {
        var command = new RouteGroupConversationMembershipDeltaCommand(
            Guid.Empty,
            [Guid.Empty],
            (DeltaOperationType)int.MaxValue,
            0);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(error => error.PropertyName).Should().BeEquivalentTo(
            nameof(command.ConversationId),
            nameof(command.ParticipantUserIds),
            nameof(command.Operation),
            nameof(command.ProjectionRevision));
    }
}
