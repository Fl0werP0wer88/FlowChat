using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandValidator : AbstractValidator<CreateGroupConversationCommand>
{
    public CreateGroupConversationCommandValidator()
    {
        RuleFor(command => command.CreatedByUserId)
            .NotEmpty()
            .WithMessage("CreatedByUserId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids is not null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");

        RuleFor(command => command.Name)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required for group conversations.");
    }
}
