using System.Net.Http.Json;
using FlowChat.Shared.Infrastructure.Http;

namespace FlowChat.GatewayService.Api.Services;

internal sealed class SocialGraphServiceClient(HttpClient httpClient)
    : ConsumerHttpClientBase(httpClient), ISocialGraphServiceClient
{
    private sealed record ContactsClientResponse(IReadOnlyList<ContactClientDto> Contacts);

    protected override string ClientDisplayName => "Social Graph Service";

    public async Task<IReadOnlyList<ContactClientDto>> GetContactsAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/contacts");
        var response = await SendAsync<ContactsClientResponse>(request, cancellationToken);
        return response?.Contacts ?? [];
    }
}
