using FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandValidatorTests
{
    private readonly AddGroupParticipantsCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        var command = new AddGroupParticipantsCommand(Guid.NewGuid(), [Guid.NewGuid()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenConversationIdIsEmpty_HasValidationError()
    {
        var command = new AddGroupParticipantsCommand(Guid.Empty, [Guid.NewGuid()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(AddGroupParticipantsCommand.ConversationId));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdsIsEmpty_HasValidationError()
    {
        var command = new AddGroupParticipantsCommand(Guid.NewGuid(), []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(AddGroupParticipantsCommand.ParticipantUserIds));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdIsEmpty_HasValidationError()
    {
        var command = new AddGroupParticipantsCommand(Guid.NewGuid(), [Guid.Empty]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName.StartsWith(nameof(AddGroupParticipantsCommand.ParticipantUserIds), StringComparison.Ordinal));
    }
}
