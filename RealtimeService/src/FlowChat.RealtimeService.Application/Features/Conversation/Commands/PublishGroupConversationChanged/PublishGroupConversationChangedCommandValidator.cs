using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Conversation.Commands.PublishGroupConversationChanged;

public sealed class PublishGroupConversationChangedCommandValidator : AbstractValidator<PublishGroupConversationChangedCommand>
{
    public PublishGroupConversationChangedCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.Type)
            .Equal(2)
            .WithMessage("Type must be Group.");

    }
}
