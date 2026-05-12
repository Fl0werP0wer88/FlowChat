using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.Presence.Commands.PublishPresenceChange;

public sealed class PublishPresenceChangeCommandValidator : AbstractValidator<PublishPresenceChangeCommand>
{
    public PublishPresenceChangeCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.Status)
            .IsInEnum()
            .WithMessage("Status must be one of: Active, AFK, Busy, Invisible.");

        RuleFor(command => command.RecipientUserIds)
            .Must(ids => ids != null && ids.Any(id => id != Guid.Empty))
            .WithMessage("RecipientUserIds must contain at least one valid user id.");
    }
}
