using System.Net.Http.Json;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class SocialGraphServiceClient : ISocialGraphServiceClient
{
    private sealed record ContactsClientResponse(IReadOnlyList<ContactClientDto> Contacts);

    private readonly HttpClient _httpClient;

    public SocialGraphServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ContactClientDto>> GetContactsAsync(CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetFromJsonAsync<ContactsClientResponse>("api/contacts", cancellationToken);
        return response?.Contacts ?? [];
    }
}
