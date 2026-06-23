using FluentValidation;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.NotificationService.Application.Features.Notification.Commands.UserEmailVerificationRequested;

public sealed class UserEmailVerificationRequestedCommandValidator
    : AbstractValidator<UserEmailVerificationRequestedCommand>
{
    public UserEmailVerificationRequestedCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.Email)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email is required.");

        RuleFor(command => command.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddress.TryCreate(value, out _))
            .WithMessage(EmailAddress.InvalidEmailAddressMessage);

        RuleFor(command => command.UserName)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("UserName is required.");

        RuleFor(command => command.ConfirmationLink)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("ConfirmationLink is required.");
    }
}
