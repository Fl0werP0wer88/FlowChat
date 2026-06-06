using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.SoftDeleteUserPresencePreferences;

public sealed class SoftDeleteUserPresencePreferencesCommandValidator
    : AbstractValidator<SoftDeleteUserPresencePreferencesCommand>
{
    public SoftDeleteUserPresencePreferencesCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
