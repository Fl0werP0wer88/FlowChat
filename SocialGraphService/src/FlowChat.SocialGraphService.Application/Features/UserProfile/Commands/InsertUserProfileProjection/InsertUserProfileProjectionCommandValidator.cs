using FluentValidation;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.InsertUserProfileProjection;

public sealed class InsertUserProfileProjectionCommandValidator : AbstractValidator<InsertUserProfileProjectionCommand>
{
    public InsertUserProfileProjectionCommandValidator()
    {
        RuleFor(command => command.UserProfileId)
            .NotEmpty()
            .WithMessage("Payload does not contain valid UserProfileId.");

        RuleFor(command => command.FriendlyUserId)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Payload does not contain valid FriendlyUserId.");
    }
}
