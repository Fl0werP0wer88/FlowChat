using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.SetMainPhone;

public sealed class SetMainPhoneCommandValidator : AbstractValidator<SetMainPhoneCommand>
{
    public SetMainPhoneCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.PhoneId)
            .NotEmpty()
            .WithMessage("PhoneId is required.");
    }
}
