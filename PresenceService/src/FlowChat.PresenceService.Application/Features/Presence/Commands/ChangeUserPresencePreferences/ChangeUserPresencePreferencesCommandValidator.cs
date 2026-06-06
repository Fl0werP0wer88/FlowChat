using FlowChat.Core.Domain;
using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserPresencePreferences;

public sealed class ChangeUserPresencePreferencesCommandValidator
    : AbstractValidator<ChangeUserPresencePreferencesCommand>
{
    public ChangeUserPresencePreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .Must(status => status is PresenceStatus.Busy or PresenceStatus.Invisible)
            .WithMessage("Only manual presence statuses (Busy, Invisible) can be saved as preferences.");
    }
}
