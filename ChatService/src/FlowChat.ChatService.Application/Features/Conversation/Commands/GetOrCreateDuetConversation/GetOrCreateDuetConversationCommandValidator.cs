using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.GetOrCreateDuetConversation;

public sealed class GetOrCreateDuetConversationCommandValidator
    : AbstractValidator<GetOrCreateDuetConversationCommand>
{
    public GetOrCreateDuetConversationCommandValidator()
    {
        RuleFor(x => x.RequestingUserId)
            .NotEmpty();

        RuleFor(x => x.PartnerUserId)
            .NotEmpty();

        RuleFor(x => x.PartnerUserId)
            .NotEqual(x => x.RequestingUserId)
            .WithMessage("PartnerUserId must differ from RequestingUserId.");
    }
}
