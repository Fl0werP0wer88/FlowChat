using System.Net.Http.Headers;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class BearerTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BearerTokenForwardingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authHeader))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
