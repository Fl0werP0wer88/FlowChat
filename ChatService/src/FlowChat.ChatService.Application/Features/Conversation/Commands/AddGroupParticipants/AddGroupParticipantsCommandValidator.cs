using FluentValidation;

namespace FlowChat.ChatService.Application.Features.Conversation.Commands.AddGroupParticipants;

public sealed class AddGroupParticipantsCommandValidator : AbstractValidator<AddGroupParticipantsCommand>
{
    public AddGroupParticipantsCommandValidator()
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
