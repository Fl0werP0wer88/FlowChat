using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishDuetConversationCreated;

public sealed class PublishDuetConversationCreatedCommandValidator : AbstractValidator<PublishDuetConversationCreatedCommand>
{
    public PublishDuetConversationCreatedCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");
    }
}
