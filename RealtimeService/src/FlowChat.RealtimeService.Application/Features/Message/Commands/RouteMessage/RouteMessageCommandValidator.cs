using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;

public sealed class RouteMessageCommandValidator : AbstractValidator<RouteMessageCommand>
{
    public RouteMessageCommandValidator()
    {
        RuleFor(command => command.MessageId)
            .NotEmpty()
            .WithMessage("MessageId is required.");

        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.SenderUserId)
            .NotEmpty()
            .WithMessage("SenderUserId is required.");

        RuleFor(command => command.SenderDisplayName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("SenderDisplayName is required.");

        RuleFor(command => command.Text)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Text is required.");

        RuleFor(command => command.RecipientUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("RecipientUserIds must contain at least one valid user id.");
    }
}
