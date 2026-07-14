using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationParticipantsRemoved;

public sealed class PublishGroupConversationParticipantsRemovedCommandValidator : AbstractValidator<PublishGroupConversationParticipantsRemovedCommand>
{
    public PublishGroupConversationParticipantsRemovedCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");
    }
}
