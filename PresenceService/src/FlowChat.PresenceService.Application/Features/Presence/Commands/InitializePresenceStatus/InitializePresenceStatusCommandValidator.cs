using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.InitializePresenceStatus;

public sealed class InitializePresenceStatusCommandValidator : AbstractValidator<InitializePresenceStatusCommand>
{
    public InitializePresenceStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}
