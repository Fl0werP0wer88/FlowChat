using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SetChatMessageSequenceNumber;

public sealed class SetChatMessageSequenceNumberCommandValidatorV2
    : AbstractValidator<SetChatMessageSequenceNumberCommandV2>
{
    public SetChatMessageSequenceNumberCommandValidatorV2()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.ConversationId).NotEmpty();
    }
}
