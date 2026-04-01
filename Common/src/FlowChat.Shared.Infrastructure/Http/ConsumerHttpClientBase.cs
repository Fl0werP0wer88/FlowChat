using System.Net;
using System.Text.Json;
using FlowChat.Core.Exceptions;

namespace FlowChat.Shared.Infrastructure.Http;

public abstract class ConsumerHttpClientBase(HttpClient httpClient)
{
    private static readonly IReadOnlySet<HttpStatusCode> DefaultNonTransientStatusCodes =
        new HashSet<HttpStatusCode>
        {
            HttpStatusCode.BadRequest,
            HttpStatusCode.Conflict,
            HttpStatusCode.Unauthorized
        };

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    protected virtual string ClientDisplayName => "Consumer API";

    protected virtual IReadOnlySet<HttpStatusCode> NonTransientStatusCodes =>
        DefaultNonTransientStatusCodes;

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
            if (await IsConcurrencyConflictAsync(response, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
            }

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

    private static async Task<bool> IsConcurrencyConflictAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.Conflict || response.Content.Headers.ContentLength == 0)
        {
            return false;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var errorProperty)
                && errorProperty.ValueKind == JsonValueKind.String
                && string.Equals(errorProperty.GetString(), "concurrency_conflict", StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
