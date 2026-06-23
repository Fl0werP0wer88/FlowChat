using System.Security.Claims;

namespace FlowChat.AuthService.Application.Features.User.Models;

public sealed class OpenIddictTokenGrantResult
{
    public required ClaimsPrincipal Principal { get; init; }
}
