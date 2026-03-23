using System.Net;
using System.Text.Json;
using FlowChat.SocialGraphService.Consumers.Configuration;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using Microsoft.Extensions.Options;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class SocialGraphInternalApiClientTests
{
    [Fact]
    public async Task UpsertUserProfileReadModelAsync_PostsToExpectedEndpointWithApiKey()
    {
        string? requestBody = null;
        var handler = new CapturingHttpMessageHandler(async (request, _) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var client = new SocialGraphInternalApiClient(
            new HttpClient(handler),
            Options.Create(new SocialGraphApiSettings
            {
                BaseUrl = "https://localhost:7194",
                ApiKey = "internal-key"
            }));

        await client.UpsertUserProfileReadModelAsync(
            new UpsertUserProfileReadModelRequest
            {
                UserProfileId = Guid.NewGuid(),
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("https://localhost:7194/internal/userprofiles/read-model", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("internal-key", handler.LastRequest.Headers.GetValues(SocialGraphInternalApiClient.ApiKeyHeaderName).Single());

        var payload = JsonSerializer.Deserialize<UpsertUserProfileReadModelRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal("jdoe", payload!.UserName);
        Assert.Equal("John Doe", payload.DisplayName);
    }

    [Fact]
    public async Task UpsertUserProfileReadModelAsync_WhenApiReturnsBadRequest_ThrowsInvalidOperationException()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            }));
        var client = new SocialGraphInternalApiClient(
            new HttpClient(handler),
            Options.Create(new SocialGraphApiSettings
            {
                BaseUrl = "https://localhost:7194"
            }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.UpsertUserProfileReadModelAsync(new UpsertUserProfileReadModelRequest(), CancellationToken.None));

        Assert.Contains("400", exception.Message);
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
