using FlowChat.Shared.Application;

namespace FlowChat.AuthService.Application.Features.Users.Commands.RefreshToken;

public class RefreshTokenCommand : ICommand<RefreshTokenCommandResponse>
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
