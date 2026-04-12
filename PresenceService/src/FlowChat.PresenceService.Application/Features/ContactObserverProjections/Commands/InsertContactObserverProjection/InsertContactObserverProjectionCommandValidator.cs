using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.InsertContactObserverProjection;

public sealed class InsertContactObserverProjectionCommandValidator : AbstractValidator<InsertContactObserverProjectionCommand>
{
    public InsertContactObserverProjectionCommandValidator()
    {
        RuleFor(x => x.ObservedUserId).NotEmpty();
        RuleFor(x => x.ObserverUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.ObservedUserId != x.ObserverUserId)
            .WithMessage("ObservedUserId and ObserverUserId must be different.");
    }
}
