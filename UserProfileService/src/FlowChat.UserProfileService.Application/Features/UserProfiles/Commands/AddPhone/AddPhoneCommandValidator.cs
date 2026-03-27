using FluentValidation;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.AddPhone;

public sealed class AddPhoneCommandValidator : AbstractValidator<AddPhoneCommand>
{
    public AddPhoneCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.Number)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Phone number is required.");

        RuleFor(command => command.Number)
            .Must(value => string.IsNullOrWhiteSpace(value) || PhoneNumber.TryCreate(value, out _))
            .WithMessage(PhoneNumber.InvalidPhoneNumberMessage);
    }
}

