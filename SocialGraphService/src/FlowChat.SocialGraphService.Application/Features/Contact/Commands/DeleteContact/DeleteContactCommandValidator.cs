using FluentValidation;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.DeleteContact;

public sealed class DeleteContactCommandValidator : AbstractValidator<DeleteContactCommand>
{
    public DeleteContactCommandValidator()
    {
        RuleFor(command => command.OwnerUserId)
            .NotEmpty()
            .WithMessage("Payload does not contain valid OwnerUserId.");

        RuleFor(command => command.ContactUserId)
            .NotEmpty()
            .WithMessage("Payload does not contain valid ContactUserId.");
    }
}
