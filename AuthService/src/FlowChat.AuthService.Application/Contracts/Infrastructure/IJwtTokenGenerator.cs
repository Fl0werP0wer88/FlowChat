using FlowChat.AuthService.Application.Users.Models;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(AuthenticatedUser user);
}
