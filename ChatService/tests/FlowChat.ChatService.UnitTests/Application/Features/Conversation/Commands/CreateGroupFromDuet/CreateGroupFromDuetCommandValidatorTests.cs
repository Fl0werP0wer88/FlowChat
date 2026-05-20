using FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;
using FluentAssertions;

namespace FlowChat.ChatService.UnitTests.Application.Features.Conversation.Commands.CreateGroupFromDuet;

public sealed class CreateGroupFromDuetCommandValidatorTests
{
    private readonly CreateGroupFromDuetCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_HasNoValidationErrors()
    {
        var command = new CreateGroupFromDuetCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNewGroupConversationIdIsEmpty_HasValidationError()
    {
        var command = new CreateGroupFromDuetCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateGroupFromDuetCommand.NewGroupConversationId));
    }

    [Fact]
    public void Validate_WhenRequestingUserIdIsEmpty_HasValidationError()
    {
        var command = new CreateGroupFromDuetCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateGroupFromDuetCommand.RequestingUserId));
    }

    [Fact]
    public void Validate_WhenPartnerUserIdIsEmpty_HasValidationError()
    {
        var command = new CreateGroupFromDuetCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(CreateGroupFromDuetCommand.PartnerUserId));
    }

    [Fact]
    public void Validate_WhenPartnerUserIdMatchesRequestingUserId_HasValidationError()
    {
        var userId = Guid.NewGuid();
        var command = new CreateGroupFromDuetCommand(Guid.NewGuid(), userId, userId);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.PropertyName == nameof(CreateGroupFromDuetCommand.PartnerUserId)
            && x.ErrorMessage == "PartnerUserId must differ from RequestingUserId.");
    }
}
