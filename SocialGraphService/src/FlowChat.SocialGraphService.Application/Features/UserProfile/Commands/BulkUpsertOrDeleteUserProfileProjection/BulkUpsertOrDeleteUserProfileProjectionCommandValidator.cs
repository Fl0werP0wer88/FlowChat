using FluentValidation;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed class BulkUpsertOrDeleteUserProfileProjectionCommandValidator
    : AbstractValidator<BulkUpsertOrDeleteUserProfileProjectionCommand>
{
    public BulkUpsertOrDeleteUserProfileProjectionCommandValidator()
    {
        RuleFor(command => command.Items)
            .NotEmpty()
            .WithMessage("Payload does not contain any user profile projection items.");

        RuleForEach(command => command.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.EntityId.Value)
                    .NotEmpty()
                    .WithMessage("Item does not contain a valid UserProfileId.");

                item.RuleFor(x => x.SourceVersion)
                    .GreaterThan(0)
                    .WithMessage("Item does not contain a valid SourceVersion.");

                item.When(i => i.Value is not null, () =>
                {
                    item.RuleFor(x => x.Value!.FriendlyUserId)
                        .Must(value => !string.IsNullOrWhiteSpace(value))
                        .WithMessage("Upsert item does not contain a valid FriendlyUserId.");

                    item.RuleFor(x => x.Value!.Source)
                        .Must(value => !string.IsNullOrWhiteSpace(value))
                        .WithMessage("Upsert item does not contain a valid Source.");
                });
            });

        RuleFor(command => command.Items)
            .Must(items => items is not null
                && items.Select(x => x.EntityId.Value).Distinct().Count() == items.Count)
            .WithMessage("Payload contains duplicate UserProfileId values.");
    }
}
