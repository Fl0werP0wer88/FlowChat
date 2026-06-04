using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;

public sealed class BulkUpsertOrDeleteUserContactProjectionCommandValidator
    : AbstractValidator<BulkUpsertOrDeleteUserContactProjectionCommand>
{
    public BulkUpsertOrDeleteUserContactProjectionCommandValidator()
    {
        RuleFor(command => command.Items)
            .NotEmpty()
            .WithMessage("Payload does not contain any contact observer projections.");

        RuleForEach(command => command.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ObservedUserId).NotEmpty();
                item.RuleFor(x => x.ObserverUserId).NotEmpty();
                item.RuleFor(x => x.SourceVersion)
                    .GreaterThan(0)
                    .WithMessage("Item does not contain a valid SourceVersion.");
                item.RuleFor(x => x)
                    .Must(x => x.ObservedUserId != x.ObserverUserId)
                    .WithMessage("ObservedUserId and ObserverUserId must be different.");

                item.When(i => i.Value is not null, () =>
                {
                    item.RuleFor(x => x.Value!.Source)
                        .Must(value => !string.IsNullOrWhiteSpace(value))
                        .WithMessage("Upsert item does not contain a valid Source.");
                });
            });

        RuleFor(command => command.Items)
            .Must(items => items is not null && items
                .Select(x => new { x.ObservedUserId, x.ObserverUserId })
                .Distinct()
                .Count() == items.Count)
            .WithMessage("Payload contains duplicate contact observer projection keys.");
    }
}
