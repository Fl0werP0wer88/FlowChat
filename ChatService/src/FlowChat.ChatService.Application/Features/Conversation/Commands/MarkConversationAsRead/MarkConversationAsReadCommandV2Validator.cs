using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.MarkConversationAsRead;

public sealed class MarkConversationAsReadCommandV2Validator
    : AbstractValidator<MarkConversationAsReadCommandV2>
{
    public MarkConversationAsReadCommandV2Validator()
    {
        RuleFor(command => command.ConversationId).NotEmpty();
        RuleFor(command => command.ParticipantUserId).NotEmpty();
        RuleFor(command => command.SequenceNum).GreaterThanOrEqualTo(0);
    }
}
