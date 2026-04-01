using FluentValidation;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddEmail;

public sealed class AddEmailCommandValidator : AbstractValidator<AddEmailCommand>
{
    public AddEmailCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.Address)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email address is required.");

        RuleFor(command => command.Address)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddress.TryCreate(value, out _))
            .WithMessage(EmailAddress.InvalidEmailAddressMessage);
    }
}

