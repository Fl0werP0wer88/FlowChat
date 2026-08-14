using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandValidatorV2Tests
{
    private readonly RemoveGroupParticipantsCommandValidatorV2 _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        var command = new RemoveGroupParticipantsCommandV2(Guid.NewGuid(), [Guid.NewGuid()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenParticipantUserIdsIsEmpty_HasValidationError()
    {
        var command = new RemoveGroupParticipantsCommandV2(Guid.NewGuid(), []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName == nameof(RemoveGroupParticipantsCommandV2.ParticipantUserIds));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdIsEmpty_HasValidationError()
    {
        var command = new RemoveGroupParticipantsCommandV2(Guid.NewGuid(), [Guid.Empty]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName.StartsWith(
                nameof(RemoveGroupParticipantsCommandV2.ParticipantUserIds),
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdsContainDuplicates_HasValidationError()
    {
        var userId = Guid.NewGuid();
        var command = new RemoveGroupParticipantsCommandV2(Guid.NewGuid(), [userId, userId]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName == nameof(RemoveGroupParticipantsCommandV2.ParticipantUserIds));
    }
}
