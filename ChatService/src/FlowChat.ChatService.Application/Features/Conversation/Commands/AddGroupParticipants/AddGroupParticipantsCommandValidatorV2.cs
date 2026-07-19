using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandValidatorV2
    : AbstractValidator<AddGroupParticipantsCommandV2>
{
    public AddGroupParticipantsCommandValidatorV2()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.ParticipantUserIds).NotEmpty();
        RuleFor(x => x.ParticipantUserIds)
            .Must(userIds => userIds.Distinct().Count() == userIds.Count)
            .WithMessage("Participant user ids cannot contain duplicates.");
        RuleForEach(x => x.ParticipantUserIds).NotEmpty();
    }
}
