using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandValidatorV2Tests
{
    private readonly AddGroupParticipantsCommandValidatorV2 _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        var command = new AddGroupParticipantsCommandV2(Guid.NewGuid(), [Guid.NewGuid()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenParticipantUserIdsIsEmpty_HasValidationError()
    {
        var command = new AddGroupParticipantsCommandV2(Guid.NewGuid(), []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName == nameof(AddGroupParticipantsCommandV2.ParticipantUserIds));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdIsEmpty_HasValidationError()
    {
        var command = new AddGroupParticipantsCommandV2(Guid.NewGuid(), [Guid.Empty]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName.StartsWith(
                nameof(AddGroupParticipantsCommandV2.ParticipantUserIds),
                StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdsContainDuplicates_HasValidationError()
    {
        var userId = Guid.NewGuid();
        var command = new AddGroupParticipantsCommandV2(Guid.NewGuid(), [userId, userId]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName == nameof(AddGroupParticipantsCommandV2.ParticipantUserIds));
    }
}
