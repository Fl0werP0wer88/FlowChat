using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationCommandValidator : AbstractValidator<ConfirmEmailVerificationCommand>
{
    public ConfirmEmailVerificationCommandValidator()
    {
        RuleFor(command => command.Token)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Token is required.");
    }
}
