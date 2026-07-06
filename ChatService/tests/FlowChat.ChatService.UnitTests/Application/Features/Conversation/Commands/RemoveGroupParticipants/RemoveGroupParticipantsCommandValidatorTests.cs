using FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandValidatorTests
{
    private readonly RemoveGroupParticipantsCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        var command = new RemoveGroupParticipantsCommand(Guid.NewGuid(), [Guid.NewGuid()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenConversationIdIsEmpty_HasValidationError()
    {
        var command = new RemoveGroupParticipantsCommand(Guid.Empty, [Guid.NewGuid()]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(RemoveGroupParticipantsCommand.ConversationId));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdsIsEmpty_HasValidationError()
    {
        var command = new RemoveGroupParticipantsCommand(Guid.NewGuid(), []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(RemoveGroupParticipantsCommand.ParticipantUserIds));
    }

    [Fact]
    public void Validate_WhenParticipantUserIdIsEmpty_HasValidationError()
    {
        var command = new RemoveGroupParticipantsCommand(Guid.NewGuid(), [Guid.Empty]);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName.StartsWith(nameof(RemoveGroupParticipantsCommand.ParticipantUserIds), StringComparison.Ordinal));
    }
}
