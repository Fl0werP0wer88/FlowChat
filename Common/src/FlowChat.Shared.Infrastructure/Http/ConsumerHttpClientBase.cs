using System.Text.Json;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Http;

namespace FlowChat.Shared.Infrastructure.Http;

public abstract class ConsumerHttpClientBase(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    protected virtual string ClientDisplayName => "Consumer API";

    protected async Task SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (HasTransientProblemDetails(body))
        {
            response.EnsureSuccessStatusCode();
        }

        throw new NonTransientException(BuildFailureMessage(response, body));
    }

    protected string BuildFailureMessage(
        HttpResponseMessage response,
        string? body)
    {
        var suffix = string.IsNullOrWhiteSpace(body)
            ? string.Empty
            : $": {body.Trim()}";

        return $"{ClientDisplayName} returned {(int)response.StatusCode} {response.ReasonPhrase}{suffix}";
    }

    private static bool HasTransientProblemDetails(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(ProblemDetailsExtensionNames.IsTransient, out var isTransientProperty)
                && isTransientProperty.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
