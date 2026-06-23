using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetMainEmail;

public sealed class SetMainEmailCommandValidator : AbstractValidator<SetMainEmailCommand>
{
    public SetMainEmailCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.EmailId)
            .NotEmpty()
            .WithMessage("EmailId is required.");
    }
}
