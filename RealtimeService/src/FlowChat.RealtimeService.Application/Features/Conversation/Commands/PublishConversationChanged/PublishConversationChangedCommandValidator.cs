using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationChanged;

public sealed class PublishConversationChangedCommandValidator : AbstractValidator<PublishConversationChangedCommand>
{
    public PublishConversationChangedCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.Type)
            .Must(type => type is 1 or 2)
            .WithMessage("Type must be one of: Duet, Group.");

        RuleFor(command => command.CreatedByUserId)
            .NotEmpty()
            .WithMessage("CreatedByUserId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");
    }
}
