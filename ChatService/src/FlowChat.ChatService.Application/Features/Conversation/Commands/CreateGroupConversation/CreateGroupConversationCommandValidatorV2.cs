using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupConversation;

public sealed class CreateGroupConversationCommandValidatorV2
    : AbstractValidator<CreateGroupConversationCommandV2>
{
    public CreateGroupConversationCommandValidatorV2()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.CreatedByUserId).NotEmpty();
        RuleFor(x => x.ParticipantUserIds).NotNull();
        RuleForEach(x => x.ParticipantUserIds).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
    }
}
