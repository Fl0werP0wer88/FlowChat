using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationMembershipDelta;

public sealed class RouteGroupConversationMembershipDeltaCommandValidator
    : AbstractValidator<RouteGroupConversationMembershipDeltaCommand>
{
    public RouteGroupConversationMembershipDeltaCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");

        RuleFor(command => command.Operation)
            .IsInEnum()
            .WithMessage("Operation must be Added or Removed.");

        RuleFor(command => command.ProjectionRevision)
            .GreaterThanOrEqualTo(1)
            .WithMessage("ProjectionRevision must be at least 1.");
    }
}
