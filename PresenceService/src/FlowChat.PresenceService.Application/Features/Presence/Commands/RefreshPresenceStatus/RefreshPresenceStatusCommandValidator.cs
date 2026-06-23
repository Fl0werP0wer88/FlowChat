using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.RefreshPresenceStatus;

public sealed class RefreshPresenceStatusCommandValidator : AbstractValidator<RefreshPresenceStatusCommand>
{
    public RefreshPresenceStatusCommandValidator()
    {
        RuleFor(x => x.UserIds).NotEmpty();
        RuleForEach(x => x.UserIds).NotEmpty();
    }
}
