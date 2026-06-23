using FlowChat.AuthService.API.Features.User.Public.RefreshTokenCookie;
using FlowChat.Shared.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowChat.AuthService.API.Features.User.Public.Logout;

[ApiController]
[Route("api/users")]
public sealed class LogoutController : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieNames.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/users"
        });

        return NoContent();
    }
}
