using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.SendChatMessage;

public sealed class SendChatMessageCommandValidatorV2
    : AbstractValidator<SendChatMessageCommandV2>
{
    public SendChatMessageCommandValidatorV2()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.SenderUserId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty();
    }
}
