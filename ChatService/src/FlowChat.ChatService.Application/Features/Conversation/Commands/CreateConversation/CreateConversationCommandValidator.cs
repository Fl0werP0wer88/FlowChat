using FluentValidation;
using FlowChat.ChatService.Domain.Entities.Conversation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateConversation;

public sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(command => command.CreatedByUserId)
            .NotEmpty()
            .WithMessage("CreatedByUserId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids is not null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");

        RuleFor(command => command.Type)
            .IsInEnum()
            .WithMessage("Conversation type is invalid.");

        When(command => command.Type == ConversationType.Group, () =>
        {
            RuleFor(command => command.Name)
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Name is required for group conversations.");
        });
    }
}
