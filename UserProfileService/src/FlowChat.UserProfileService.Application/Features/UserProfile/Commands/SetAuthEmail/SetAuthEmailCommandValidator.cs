using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.SetAuthEmail;

public sealed class SetAuthEmailCommandValidator : AbstractValidator<SetAuthEmailCommand>
{
    public SetAuthEmailCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.EmailId)
            .NotEmpty()
            .WithMessage("EmailId is required.");
    }
}
