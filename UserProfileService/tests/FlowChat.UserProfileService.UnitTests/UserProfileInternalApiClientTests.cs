using System.Net;
using System.Text.Json;
using FlowChat.Core.Exceptions;
using FlowChat.UserProfileService.Consumers.Configuration;
using FlowChat.UserProfileService.Consumers.Services;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;
using Microsoft.Extensions.Options;

namespace FlowChat.UserProfileService.UnitTests;

public sealed class UserProfileInternalApiClientTests
{
    [Fact]
    public async Task CreateInitialUserProfileAsync_PostsToExpectedEndpointWithApiKey()
    {
        string? requestBody = null;
        var handler = new CapturingHttpMessageHandler(async (request, _) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var client = new UserProfileInternalApiClient(
            new HttpClient(handler),
            Options.Create(new UserProfileApiSettings
            {
                BaseUrl = "https://localhost:7148",
                ApiKey = "internal-key"
            }));

        await client.CreateInitialUserProfileAsync(
            new CreateInitialUserProfileRequest
            {
                UserId = Guid.NewGuid(),
                UserName = "jdoe",
                DisplayName = "John Doe",
                Email = "john@example.com",
                Phone = "+48123123123"
            },
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://localhost:7148/internal/userprofiles/initial", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("internal-key", handler.LastRequest.Headers.GetValues(UserProfileInternalApiClient.ApiKeyHeaderName).Single());

        var payload = JsonSerializer.Deserialize<CreateInitialUserProfileRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal("jdoe", payload!.UserName);
        Assert.Equal("John Doe", payload.DisplayName);
    }

    [Fact]
    public async Task CreateInitialUserProfileAsync_WhenApiReturnsConflict_ThrowsNonTransientException()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("conflict")
            }));
        var client = new UserProfileInternalApiClient(
            new HttpClient(handler),
            Options.Create(new UserProfileApiSettings
            {
                BaseUrl = "https://localhost:7148"
            }));

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.CreateInitialUserProfileAsync(new CreateInitialUserProfileRequest(), CancellationToken.None));

        Assert.Contains("409", exception.Message);
    }

    private sealed class CapturingHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responseFactory = responseFactory;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return await _responseFactory(request, cancellationToken);
        }
    }
}
