using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.DeleteContactObserverProjection;

public sealed class DeleteContactObserverProjectionCommandValidator : AbstractValidator<DeleteContactObserverProjectionCommand>
{
    public DeleteContactObserverProjectionCommandValidator()
    {
        RuleFor(x => x.ObservedUserId).NotEmpty();
        RuleFor(x => x.ObserverUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.ObservedUserId != x.ObserverUserId)
            .WithMessage("ObservedUserId and ObserverUserId must be different.");
    }
}
