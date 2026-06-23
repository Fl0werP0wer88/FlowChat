using Microsoft.Extensions.Options;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace FlowChat.AuthService.API.Features.User.Public.RefreshTokenCookie;

public sealed class SetRefreshTokenCookieHandler : IOpenIddictServerHandler<ApplyTokenResponseContext>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOptions<OpenIddictServerOptions> _serverOptions;

    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<ApplyTokenResponseContext>()
            .UseSingletonHandler<SetRefreshTokenCookieHandler>()
            // Runs before the ASP.NET Core handler that writes the HTTP response (~100_000)
            .SetOrder(50_000)
            .Build();

    public SetRefreshTokenCookieHandler(
        IHttpContextAccessor httpContextAccessor,
        IOptions<OpenIddictServerOptions> serverOptions)
    {
        _httpContextAccessor = httpContextAccessor;
        _serverOptions = serverOptions;
    }

    public ValueTask HandleAsync(ApplyTokenResponseContext context)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var refreshToken = context.Response.RefreshToken;

        if (httpContext is null || string.IsNullOrEmpty(refreshToken))
        {
            return ValueTask.CompletedTask;
        }

        var lifetime = _serverOptions.Value.RefreshTokenLifetime ?? TimeSpan.FromDays(14);

        httpContext.Response.Cookies.Append(CookieNames.RefreshToken, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            // Scoped to auth endpoints so the cookie is never sent to other routes
            Path = "/api/users",
            Expires = DateTimeOffset.UtcNow.Add(lifetime)
        });

        // Remove from the JSON body — the HttpOnly cookie is the sole delivery channel
        context.Response.RefreshToken = null;

        return ValueTask.CompletedTask;
    }
}
