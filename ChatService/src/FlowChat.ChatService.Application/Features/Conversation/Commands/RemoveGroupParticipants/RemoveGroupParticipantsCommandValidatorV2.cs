using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandValidatorV2
    : AbstractValidator<RemoveGroupParticipantsCommandV2>
{
    public RemoveGroupParticipantsCommandValidatorV2()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.ParticipantUserIds).NotEmpty();
        RuleFor(x => x.ParticipantUserIds)
            .Must(userIds => userIds.Distinct().Count() == userIds.Count)
            .WithMessage("Participant user ids cannot contain duplicates.");
        RuleForEach(x => x.ParticipantUserIds).NotEmpty();
    }
}
