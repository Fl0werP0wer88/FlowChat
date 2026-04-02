using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SendEmailVerification;

public sealed class SendEmailVerificationCommandValidator : AbstractValidator<SendEmailVerificationCommand>
{
    public SendEmailVerificationCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.EmailId)
            .NotEmpty()
            .WithMessage("EmailId is required.");
    }
}
