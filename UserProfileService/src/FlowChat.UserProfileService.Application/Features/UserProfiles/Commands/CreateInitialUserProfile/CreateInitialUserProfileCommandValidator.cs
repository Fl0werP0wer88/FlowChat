using FluentValidation;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileCommandValidator
    : AbstractValidator<CreateInitialUserProfileCommand>
{
    public CreateInitialUserProfileCommandValidator()
    {
        RuleFor(command => command.UserName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("UserName is required.");

        RuleFor(command => command.DisplayName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("DisplayName is required.");

        RuleFor(command => command.Email)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email is required.");

        RuleFor(command => command.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddress.TryCreate(value, out _))
            .WithMessage(EmailAddress.InvalidEmailAddressMessage);

        RuleFor(command => command.Phone)
            .Must(value => string.IsNullOrWhiteSpace(value) || PhoneNumber.TryCreate(value, out _))
            .WithMessage(PhoneNumber.InvalidPhoneNumberMessage);
    }
}

