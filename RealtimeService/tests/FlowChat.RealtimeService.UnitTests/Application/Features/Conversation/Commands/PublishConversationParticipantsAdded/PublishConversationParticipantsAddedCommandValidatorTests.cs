using AutoFixture;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsAdded;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Application.Features.Conversation.Commands.PublishConversationParticipantsAdded;

public sealed class PublishConversationParticipantsAddedCommandValidatorTests
{
    private readonly IFixture _fixture = new Fixture();
    private readonly PublishConversationParticipantsAddedCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidParticipantState_ReturnsNoValidationErrors()
    {
        var result = _validator.Validate(CreateCommand(4, 7));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeParticipantCount_ReturnsValidationError()
    {
        var result = _validator.Validate(CreateCommand(-1, 7));

        result.Errors.Should().Contain(error => error.PropertyName == "ParticipantCount");
    }

    [Fact]
    public void Validate_MembershipRevisionBelowProjectionBaseline_ReturnsValidationError()
    {
        var result = _validator.Validate(CreateCommand(4, 1));

        result.Errors.Should().Contain(error => error.PropertyName == "MembershipRevision");
    }

    private PublishConversationParticipantsAddedCommand CreateCommand(
        int participantCount,
        int membershipRevision) =>
        new(
            _fixture.Create<Guid>(),
            2,
            [_fixture.Create<Guid>()],
            participantCount,
            membershipRevision);
}
