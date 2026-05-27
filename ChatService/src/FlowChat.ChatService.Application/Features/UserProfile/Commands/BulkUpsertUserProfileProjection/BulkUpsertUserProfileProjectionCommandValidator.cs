using FluentValidation;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertUserProfileProjection;

public sealed class BulkUpsertUserProfileProjectionCommandValidator
    : AbstractValidator<BulkUpsertUserProfileProjectionCommand>
{
    public BulkUpsertUserProfileProjectionCommandValidator()
    {
        RuleFor(command => command.Items)
            .NotEmpty()
            .WithMessage("Payload does not contain any user profile projections.");

        RuleForEach(command => command.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.UserProfileId)
                    .NotEmpty()
                    .WithMessage("Payload does not contain valid UserProfileId.");

                item.RuleFor(x => x.FriendlyUserId)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage("Payload does not contain valid FriendlyUserId.");

                item.RuleFor(x => x.CreatedBy)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage("Payload does not contain valid CreatedBy.");

                item.RuleFor(x => x.CreatedAtUtc)
                    .NotEmpty()
                    .WithMessage("Payload does not contain valid CreatedAtUtc.");

                item.RuleFor(x => x.LastModifiedBy)
                    .Must(value => !string.IsNullOrWhiteSpace(value))
                    .WithMessage("Payload does not contain valid LastModifiedBy.");

                item.RuleFor(x => x.LastModifiedAtUtc)
                    .NotEmpty()
                    .WithMessage("Payload does not contain valid LastModifiedAtUtc.");
            });

        RuleFor(command => command.Items)
            .Must(items => items is not null && items.Select(x => x.UserProfileId).Distinct().Count() == items.Count)
            .WithMessage("Payload contains duplicate UserProfileId values.");
    }
}
