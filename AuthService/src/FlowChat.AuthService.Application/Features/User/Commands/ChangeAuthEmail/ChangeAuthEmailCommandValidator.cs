using FluentValidation;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Application.Features.User.Commands.ChangeAuthEmail;

public sealed class ChangeAuthEmailCommandValidator : AbstractValidator<ChangeAuthEmailCommand>
{
    public ChangeAuthEmailCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.EmailAddress)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Email address is required.");

        RuleFor(command => command.EmailAddress)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddress.TryCreate(value, out _))
            .WithMessage(EmailAddress.InvalidEmailAddressMessage);
    }
}
