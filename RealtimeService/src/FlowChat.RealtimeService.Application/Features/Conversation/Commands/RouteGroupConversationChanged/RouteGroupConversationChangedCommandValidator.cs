using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;

public sealed class RouteGroupConversationChangedCommandValidator : AbstractValidator<RouteGroupConversationChangedCommand>
{
    public RouteGroupConversationChangedCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.Type)
            .Equal(2)
            .WithMessage("Type must be Group.");

        RuleFor(command => command.CreatedByUserId)
            .NotEmpty()
            .WithMessage("CreatedByUserId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");
    }
}
