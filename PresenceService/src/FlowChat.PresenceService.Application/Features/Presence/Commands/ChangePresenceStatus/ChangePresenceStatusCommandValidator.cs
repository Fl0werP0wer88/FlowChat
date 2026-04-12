using FlowChat.Core.Domain;
using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangePresenceStatus;

public sealed class ChangePresenceStatusCommandValidator : AbstractValidator<ChangePresenceStatusCommand>
{
    public ChangePresenceStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .Must(status => Enum.IsDefined(typeof(PresenceStatus), status))
            .WithMessage("Status is invalid.");
    }
}
