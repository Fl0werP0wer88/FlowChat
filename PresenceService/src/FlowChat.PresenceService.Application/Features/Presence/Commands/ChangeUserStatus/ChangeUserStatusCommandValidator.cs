using FlowChat.Core.Domain;
using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.Presence.Commands.ChangeUserStatus;

public sealed class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
{
    public ChangeUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Status)
            .Must(status => Enum.IsDefined(typeof(PresenceStatus), status))
            .WithMessage("Status is invalid.");
    }
}
