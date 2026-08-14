using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed class CreateDuetConversationCommandValidatorV2
    : AbstractValidator<CreateDuetConversationCommandV2>
{
    public CreateDuetConversationCommandValidatorV2()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.PartnerUserId).NotEmpty();
        RuleFor(x => x.PartnerUserId).NotEqual(x => x.RequestingUserId);
    }
}
