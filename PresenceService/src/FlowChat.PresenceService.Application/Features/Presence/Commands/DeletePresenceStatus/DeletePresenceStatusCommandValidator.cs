using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.DeletePresenceStatus;

public sealed class DeletePresenceStatusCommandValidator : AbstractValidator<DeletePresenceStatusCommand>
{
    public DeletePresenceStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
