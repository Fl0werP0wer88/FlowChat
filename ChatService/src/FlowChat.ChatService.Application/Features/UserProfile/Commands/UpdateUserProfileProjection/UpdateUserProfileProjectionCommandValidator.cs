using FluentValidation;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.UpdateUserProfileProjection;

public sealed class UpdateUserProfileProjectionCommandValidator : AbstractValidator<UpdateUserProfileProjectionCommand>
{
    public UpdateUserProfileProjectionCommandValidator()
    {
        RuleFor(command => command.UserProfileId)
            .NotEmpty()
            .WithMessage("Payload does not contain valid UserProfileId.");

        RuleFor(command => command.FriendlyUserId)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Payload does not contain valid FriendlyUserId.");

        RuleFor(command => command.CreatedBy)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Payload does not contain valid CreatedBy.");

        RuleFor(command => command.CreatedAtUtc)
            .NotEmpty()
            .WithMessage("Payload does not contain valid CreatedAtUtc.");

        RuleFor(command => command.LastModifiedBy)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Payload does not contain valid LastModifiedBy.");

        RuleFor(command => command.LastModifiedAtUtc)
            .NotEmpty()
            .WithMessage("Payload does not contain valid LastModifiedAtUtc.");
    }
}
