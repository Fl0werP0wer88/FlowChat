using FlowChat.AuthService.Application.Models;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(AuthenticatedUser user);
}
