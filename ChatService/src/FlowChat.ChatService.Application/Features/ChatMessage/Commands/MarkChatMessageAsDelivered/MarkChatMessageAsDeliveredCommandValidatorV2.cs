using FluentValidation;

namespace FlowChat.ChatService.Application.Features.ChatMessage.Commands.MarkChatMessageAsDelivered;

public sealed class MarkChatMessageAsDeliveredCommandValidatorV2
    : AbstractValidator<MarkChatMessageAsDeliveredCommandV2>
{
    public MarkChatMessageAsDeliveredCommandValidatorV2()
    {
        RuleFor(x => x.MessageId).NotEmpty();
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.DeliveredAtUtc).NotEmpty();
    }
}
