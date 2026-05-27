using FluentValidation;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertContactObserverProjection;

public sealed class BulkUpsertContactObserverProjectionCommandValidator
    : AbstractValidator<BulkUpsertContactObserverProjectionCommand>
{
    public BulkUpsertContactObserverProjectionCommandValidator()
    {
        RuleFor(command => command.Items)
            .NotEmpty()
            .WithMessage("Payload does not contain any contact observer projections.");

        RuleForEach(command => command.Items)
            .ChildRules(item =>
            {
                item.RuleFor(x => x.ObservedUserId).NotEmpty();
                item.RuleFor(x => x.ObserverUserId).NotEmpty();
                item.RuleFor(x => x)
                    .Must(x => x.ObservedUserId != x.ObserverUserId)
                    .WithMessage("ObservedUserId and ObserverUserId must be different.");
            });

        RuleFor(command => command.Items)
            .Must(items => items is not null && items
                .Select(x => new { x.ObservedUserId, x.ObserverUserId })
                .Distinct()
                .Count() == items.Count)
            .WithMessage("Payload contains duplicate contact observer projection keys.");
    }
}
