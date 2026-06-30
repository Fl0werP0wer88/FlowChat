using FluentValidation;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Application.Features.User.Commands.ConfirmAuthEmail;

public sealed class ConfirmAuthEmailCommandValidator : AbstractValidator<ConfirmAuthEmailCommand>
{
    public ConfirmAuthEmailCommandValidator()
    {
        RuleFor(command => command.EmailAddress)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email address is required.");

        RuleFor(command => command.EmailAddress)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddress.TryCreate(value, out _))
            .WithMessage(EmailAddress.InvalidEmailAddressMessage);
    }
}
