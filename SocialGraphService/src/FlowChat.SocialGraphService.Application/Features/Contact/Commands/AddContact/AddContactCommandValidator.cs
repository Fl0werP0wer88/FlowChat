using FlowChat.Shared.Domain.ValueObjects;
using FluentValidation;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;

public sealed class AddContactCommandValidator : AbstractValidator<AddContactCommand>
{
    public AddContactCommandValidator()
    {
        RuleFor(command => command.OwnerUserId)
            .NotEmpty()
            .WithMessage("Payload does not contain valid OwnerUserId.");

        RuleFor(command => command.UserId)
            .Must(userId => !userId.HasValue || userId.Value != Guid.Empty)
            .WithMessage("Payload does not contain valid UserId.");

        RuleFor(command => command.FriendlyUserId)
            .MaximumLength(100)
            .When(command => !string.IsNullOrWhiteSpace(command.FriendlyUserId))
            .WithMessage("Payload FriendlyUserId cannot be longer than 100 characters.");

        RuleFor(command => command.Email)
            .Must(email => string.IsNullOrWhiteSpace(email) || EmailAddress.TryCreate(email, out _))
            .WithMessage("Payload does not contain valid Email.");

        RuleFor(command => command)
            .Must(HasExactlyOneIdentifier)
            .WithMessage("Payload must contain exactly one of: UserId, FriendlyUserId or Email.");
    }

    private static bool HasExactlyOneIdentifier(AddContactCommand command)
    {
        var providedIdentifiers = 0;

        if (command.UserId.HasValue)
        {
            providedIdentifiers++;
        }

        if (!string.IsNullOrWhiteSpace(command.FriendlyUserId))
        {
            providedIdentifiers++;
        }

        if (!string.IsNullOrWhiteSpace(command.Email))
        {
            providedIdentifiers++;
        }

        return providedIdentifiers == 1;
    }
}
