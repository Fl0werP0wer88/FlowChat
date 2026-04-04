using FlowChat.Shared.Application;

namespace FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;

public sealed class RefreshTokenCommand : ICommand<RefreshTokenCommandResponse>
{
    public required Guid AccountId { get; set; }
    public IReadOnlyCollection<string> Scopes { get; set; } = [];
}
