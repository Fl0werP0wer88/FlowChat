using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
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
    }
}
