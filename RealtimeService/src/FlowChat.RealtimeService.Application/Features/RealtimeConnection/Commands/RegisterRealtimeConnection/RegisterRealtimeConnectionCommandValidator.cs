using FluentValidation;

namespace FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands.RegisterRealtimeConnection;

public sealed class RegisterRealtimeConnectionCommandValidator : AbstractValidator<RegisterRealtimeConnectionCommand>
{
    public RegisterRealtimeConnectionCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");

        RuleFor(command => command.ConnectionId)
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("ConnectionId is required.");
    }
}
