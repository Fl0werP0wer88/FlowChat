using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.RemoveGroupParticipants;

public sealed class RemoveGroupParticipantsCommandValidator : AbstractValidator<RemoveGroupParticipantsCommand>
{
    public RemoveGroupParticipantsCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("ConversationId is required.");

        RuleFor(command => command.ParticipantUserIds)
            .NotEmpty()
            .WithMessage("At least one ParticipantUserId is required.");

        RuleForEach(command => command.ParticipantUserIds)
            .NotEmpty()
            .WithMessage("Each ParticipantUserId must be a non-empty GUID.");
    }
}
