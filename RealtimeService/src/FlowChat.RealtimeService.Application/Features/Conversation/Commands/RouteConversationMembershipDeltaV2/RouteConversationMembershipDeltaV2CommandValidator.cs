using FlowChat.Core.Messaging;
using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;

public sealed class RouteConversationMembershipDeltaV2CommandValidator
    : AbstractValidator<RouteConversationMembershipDeltaV2Command>
{
    public RouteConversationMembershipDeltaV2CommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.ConversationType)
            .Must(type => type is 1 or 2)
            .WithMessage("ConversationType must be Duet or Group.");

        RuleFor(command => command.ProjectionRevision)
            .GreaterThanOrEqualTo(2)
            .WithMessage("ProjectionRevision must be at least 2.");

        RuleFor(command => command.Delta)
            .NotNull()
            .NotEmpty()
            .WithMessage("Delta must contain at least one item.");

        RuleForEach(command => command.Delta)
            .ChildRules(item =>
            {
                item.RuleFor(value => value.ParticipantUserId)
                    .NotEmpty()
                    .WithMessage("ParticipantUserId is required.");

                item.RuleFor(value => value.Operation)
                    .Must(operation => operation is OperationType.Created or OperationType.Deleted)
                    .WithMessage("Operation must be Created or Deleted.");
            });

        RuleFor(command => command.Delta)
            .Must(HaveUniqueParticipantUserIds)
            .WithMessage("Delta must not contain duplicate or conflicting operations for a participant.");
    }

    private static bool HaveUniqueParticipantUserIds(
        IReadOnlyCollection<ConversationMembershipDeltaItemV2>? delta)
    {
        if (delta is null)
        {
            return true;
        }

        return delta
            .Select(item => item.ParticipantUserId)
            .Distinct()
            .Count() == delta.Count;
    }
}
