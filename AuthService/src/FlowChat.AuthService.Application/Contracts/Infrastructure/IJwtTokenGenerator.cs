using FlowChat.AuthService.Application.Features.Users.Models;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IJwtTokenGenerator
{
    RefreshTokenResult GenerateRefreshToken();
    JwtTokenResult GenerateToken(AuthenticatedUser user);
    JwtTokenResult GenerateToken(AuthenticatedUser user, string refreshToken, DateTime refreshTokenExpiresAtUtc);
    Guid? ExtractUserIdFromExpiredToken(string accessToken);
}
