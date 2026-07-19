using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;

public sealed class CreateGroupFromDuetCommandValidatorV2
    : AbstractValidator<CreateGroupFromDuetCommandV2>
{
    public CreateGroupFromDuetCommandValidatorV2()
    {
        RuleFor(x => x.NewGroupConversationId).NotEmpty();
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.PartnerUserId).NotEmpty();
        RuleFor(x => x.PartnerUserId).NotEqual(x => x.RequestingUserId);
    }
}
