using FlowChat.AuthService.Application.Features.Users.Models;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IJwtTokenGenerator
{
    JwtTokenResult GenerateToken(AuthenticatedUser user);
    Guid? ExtractUserIdFromExpiredToken(string accessToken);
}
