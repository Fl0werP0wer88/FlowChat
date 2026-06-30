using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Http;
using FlowChat.Shared.Domain;

namespace FlowChat.Shared.Infrastructure.Http;

public abstract class FlowChatHttpClientBase(HttpClient httpClient)
{
    public const string InternalApiKeyHeaderName = "X-Internal-Api-Key";

    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

    protected virtual string ClientDisplayName => "Consumer API";

    protected virtual JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
            return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, cancellationToken);
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

        HttpRequestException inner;
        try
        {
            response.EnsureSuccessStatusCode();
            return;
        }
        catch (HttpRequestException ex)
        {
            inner = ex;
        }

        var message = BuildFailureMessage(response, body);

        var failureKind = GetFailureKind(body);

        if (failureKind == FailureKind.Transient || IsTransientStatusCode(response))
        {
            throw new TransientException(message, inner);
        }

        if (failureKind == FailureKind.Isolable)
        {
            throw new IsolableException(message, inner);
        }

        throw new NonTransientException(message, inner);
    }

    private static bool IsTransientStatusCode(HttpResponseMessage response) =>
        response.StatusCode is HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.BadGateway
            or HttpStatusCode.GatewayTimeout;

    private static FailureKind GetFailureKind(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return FailureKind.None;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty(ProblemDetailsExtensionNames.FailureKind, out var kindProperty)
                && kindProperty.ValueKind == JsonValueKind.String
                && Enum.TryParse<FailureKind>(kindProperty.GetString(), out var parsed))
            {
                return parsed;
            }
        }
        catch (JsonException)
        {
        }

        return FailureKind.None;
    }
}
