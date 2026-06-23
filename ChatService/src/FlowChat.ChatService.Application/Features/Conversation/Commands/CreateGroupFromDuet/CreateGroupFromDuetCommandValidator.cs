using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.CreateGroupFromDuet;

public sealed class CreateGroupFromDuetCommandValidator
    : AbstractValidator<CreateGroupFromDuetCommand>
{
    public CreateGroupFromDuetCommandValidator()
    {
        RuleFor(x => x.NewGroupConversationId)
            .NotEmpty();

        RuleFor(x => x.RequestingUserId)
            .NotEmpty();

        RuleFor(x => x.PartnerUserId)
            .NotEmpty();

        RuleFor(x => x.PartnerUserId)
            .NotEqual(x => x.RequestingUserId)
            .WithMessage("PartnerUserId must differ from RequestingUserId.");
    }
}
