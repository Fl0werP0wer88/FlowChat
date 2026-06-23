using FluentValidation;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    private const int FirstNameMaxLength = 100;
    private const int LastNameMaxLength = 100;
    private const int OrganizationMaxLength = 200;
    private const int AvatarUrlMaxLength = 500;
    private const int BioMaxLength = 500;

    public UpdateProfileCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.FirstName)
            .MaximumLength(FirstNameMaxLength)
            .When(command => command.FirstName is not null)
            .WithMessage($"FirstName must not exceed {FirstNameMaxLength} characters.");

        RuleFor(command => command.LastName)
            .MaximumLength(LastNameMaxLength)
            .When(command => command.LastName is not null)
            .WithMessage($"LastName must not exceed {LastNameMaxLength} characters.");

        RuleFor(command => command.Organization)
            .MaximumLength(OrganizationMaxLength)
            .When(command => command.Organization is not null)
            .WithMessage($"Organization must not exceed {OrganizationMaxLength} characters.");

        RuleFor(command => command.AvatarUrl)
            .MaximumLength(AvatarUrlMaxLength)
            .When(command => command.AvatarUrl is not null)
            .WithMessage($"AvatarUrl must not exceed {AvatarUrlMaxLength} characters.");

        RuleFor(command => command.Bio)
            .MaximumLength(BioMaxLength)
            .When(command => command.Bio is not null)
            .WithMessage($"Bio must not exceed {BioMaxLength} characters.");
    }
}
