using FluentValidation;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileCommandValidator
    : AbstractValidator<CreateInitialUserProfileCommand>
{
    public CreateInitialUserProfileCommandValidator()
    {
        RuleFor(command => command.FriendlyUserId)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("FriendlyUserId is required.");

        RuleFor(command => command.Email)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email is required.");

        RuleFor(command => command.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddress.TryCreate(value, out _))
            .WithMessage(EmailAddress.InvalidEmailAddressMessage);
    }
}

