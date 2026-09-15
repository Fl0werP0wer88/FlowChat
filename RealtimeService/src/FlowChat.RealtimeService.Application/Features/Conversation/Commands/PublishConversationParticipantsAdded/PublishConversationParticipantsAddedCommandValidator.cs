using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishConversationParticipantsAdded;

public sealed class PublishConversationParticipantsAddedCommandValidator : AbstractValidator<PublishConversationParticipantsAddedCommand>
{
    public PublishConversationParticipantsAddedCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.ConversationType)
            .Must(type => type is 1 or 2)
            .WithMessage("ConversationType must be Duet or Group.");

        RuleFor(command => command.ParticipantUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("ParticipantUserIds must contain at least one valid user id.");

        RuleFor(command => command.ParticipantCount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("ParticipantCount cannot be negative.");

        RuleFor(command => command.MembershipRevision)
            .GreaterThanOrEqualTo(2)
            .WithMessage("MembershipRevision must be at least 2.");
    }
}
