using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandValidatorV2
    : AbstractValidator<AddGroupParticipantsCommandV2>
{
    public AddGroupParticipantsCommandValidatorV2()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.ParticipantUserIds).NotEmpty();
        RuleForEach(x => x.ParticipantUserIds).NotEmpty();
    }
}
