using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;

public sealed class RouteConversationProjectionV2CommandValidator
    : AbstractValidator<RouteConversationProjectionV2Command>
{
    private const int DuetConversationType = 1;
    private const int GroupConversationType = 2;

    public RouteConversationProjectionV2CommandValidator()
    {
        RuleFor(command => command.SourceAggregateId)
            .NotEmpty()
            .WithMessage("SourceAggregateId is required.");

        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .Equal(command => command.SourceAggregateId)
            .WithMessage("ConversationId must match SourceAggregateId.");

        RuleFor(command => command.ConversationType)
            .Must(type => type is DuetConversationType or GroupConversationType)
            .WithMessage("ConversationType must be Duet or Group.");

        RuleFor(command => command.Operation)
            .IsInEnum()
            .WithMessage("Operation is invalid.");
    }
}
