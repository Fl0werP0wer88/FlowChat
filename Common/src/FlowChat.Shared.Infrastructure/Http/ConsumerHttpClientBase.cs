using System.Net;
using FlowChat.Core.Exceptions;

namespace FlowChat.Shared.Infrastructure.Http;

public abstract class ConsumerHttpClientBase(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    protected virtual string ClientDisplayName => "Consumer API";

    protected virtual IReadOnlySet<HttpStatusCode> NonTransientStatusCodes =>
        new HashSet<HttpStatusCode>();

    protected async Task SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if (NonTransientStatusCodes.Contains(response.StatusCode))
        {
            throw new NonTransientException(await BuildFailureMessageAsync(response, cancellationToken));
        }

        response.EnsureSuccessStatusCode();
    }

    protected async Task<string> BuildFailureMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var suffix = string.IsNullOrWhiteSpace(body)
            ? string.Empty
            : $": {body.Trim()}";

        return $"{ClientDisplayName} returned {(int)response.StatusCode} {response.ReasonPhrase}{suffix}";
    }
}
