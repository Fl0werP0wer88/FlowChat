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
            .Must(status => status is PresenceStatus.Active or PresenceStatus.Busy or PresenceStatus.Invisible)
            .WithMessage("AFK cannot be saved as a default startup status.");
    }
}
