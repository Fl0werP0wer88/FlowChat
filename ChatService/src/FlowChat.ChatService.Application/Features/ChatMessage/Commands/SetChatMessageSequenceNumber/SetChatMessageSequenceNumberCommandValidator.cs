using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberCommandValidator : AbstractValidator<SetChatMessageSequenceNumberCommand>
{
    public SetChatMessageSequenceNumberCommandValidator()
    {
        RuleFor(command => command.MessageId)
            .NotEmpty()
            .WithMessage("MessageId is required.");

        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");
    }
}
