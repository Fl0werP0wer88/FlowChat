using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.UnregisterRealtimeConnection;

public sealed class UnregisterRealtimeConnectionCommandValidator : AbstractValidator<UnregisterRealtimeConnectionCommand>
{
    public UnregisterRealtimeConnectionCommandValidator()
    {
        RuleFor(command => command.ConnectionId)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("ConnectionId is required.");
    }
}
