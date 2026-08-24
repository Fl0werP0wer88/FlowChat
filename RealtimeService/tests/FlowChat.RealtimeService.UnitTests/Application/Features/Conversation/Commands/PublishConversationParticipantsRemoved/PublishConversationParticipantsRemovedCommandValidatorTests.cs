using AutoFixture;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsRemoved;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Application.Features.Conversation.Commands.PublishConversationParticipantsRemoved;

public sealed class PublishConversationParticipantsRemovedCommandValidatorTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly PublishConversationParticipantsRemovedCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidParticipantState_ReturnsNoValidationErrors()
    {
        var result = _validator.Validate(CreateCommand(3, 8));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeParticipantCount_ReturnsValidationError()
    {
        var result = _validator.Validate(CreateCommand(-1, 8));

        result.Errors.Should().Contain(error => error.PropertyName == "ParticipantCount");
    }

    [Fact]
    public void Validate_MembershipRevisionBelowProjectionBaseline_ReturnsValidationError()
    {
        var result = _validator.Validate(CreateCommand(3, 1));

        result.Errors.Should().Contain(error => error.PropertyName == "MembershipRevision");
    }

    private PublishConversationParticipantsRemovedCommand CreateCommand(
        int participantCount,
        int membershipRevision) =>
        new(
            _fixture.Create<Guid>(),
            2,
            [_fixture.Create<Guid>()],
            participantCount,
            membershipRevision);
}
