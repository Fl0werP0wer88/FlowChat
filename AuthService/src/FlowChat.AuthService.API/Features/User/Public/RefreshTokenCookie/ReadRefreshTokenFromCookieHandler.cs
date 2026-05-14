using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace FlowChat.AuthService.API.Features.User.Public.RefreshTokenCookie;

public sealed class ReadRefreshTokenFromCookieHandler : IOpenIddictServerHandler<ExtractTokenRequestContext>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor.CreateBuilder<ExtractTokenRequestContext>()
            .UseSingletonHandler<ReadRefreshTokenFromCookieHandler>()
            // Runs after the default ASP.NET Core form extraction (~100_000) so form data takes precedence
            .SetOrder(200_000)
            .Build();

    public ReadRefreshTokenFromCookieHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ValueTask HandleAsync(ExtractTokenRequestContext context)
    {
        if (!string.Equals(context.Request?.GrantType,
                OpenIddictConstants.GrantTypes.RefreshToken,
                StringComparison.Ordinal)
            || !string.IsNullOrEmpty(context.Request?.RefreshToken))
        {
            return ValueTask.CompletedTask;
        }

        var cookieToken = _httpContextAccessor.HttpContext?.Request.Cookies[CookieNames.RefreshToken];

        if (!string.IsNullOrEmpty(cookieToken))
        {
            context.Request!.RefreshToken = cookieToken;
        }

        return ValueTask.CompletedTask;
    }
}
