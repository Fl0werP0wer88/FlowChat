using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateDuetConversation;

public sealed class CreateDuetConversationCommandValidator
    : AbstractValidator<CreateDuetConversationCommand>
{
    public CreateDuetConversationCommandValidator()
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
