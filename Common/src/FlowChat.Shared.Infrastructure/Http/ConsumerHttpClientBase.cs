using System.Net;
using System.Net.Http.Json;
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
        if (!response.IsSuccessStatusCode)
        {
            await ThrowForErrorAsync(response, cancellationToken);
        }
    }

    protected async Task<TResponse?> SendAsync<TResponse>(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken);
        }

        await ThrowForErrorAsync(response, cancellationToken);
        return default;
    }

    protected async Task<TResponse?> SendAsync<TResponse>(
        HttpRequestMessage request,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<TResponse>(jsonOptions, cancellationToken);
        }

        await ThrowForErrorAsync(response, cancellationToken);
        return default;
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

    private async Task ThrowForErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = response.Content is null
            ? null
            : await response.Content.ReadAsStringAsync(cancellationToken);

        if (HasTransientProblemDetails(body) || IsTransientStatusCode(response))
        {
            response.EnsureSuccessStatusCode();
        }

        throw new NonTransientException(BuildFailureMessage(response, body));
    }

    private static bool IsTransientStatusCode(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.GatewayTimeout;

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
