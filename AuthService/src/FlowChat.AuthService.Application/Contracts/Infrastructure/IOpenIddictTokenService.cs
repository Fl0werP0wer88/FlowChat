using System.Security.Claims;
using FlowChat.AuthService.Application.Features.User.Models;

namespace FlowChat.AuthService.Application.Contracts.Infrastructure;

public interface IOpenIddictTokenService
{
    ClaimsPrincipal CreatePrincipal(AuthenticatedAccount account, IEnumerable<string> scopes);
}
