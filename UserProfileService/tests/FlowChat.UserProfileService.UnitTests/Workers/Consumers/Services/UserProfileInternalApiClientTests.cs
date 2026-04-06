using System.Net;
using System.Text;
using System.Text.Json;
using FlowChat.Core.Exceptions;
using FlowChat.UserProfileService.Consumers.Services;
using FlowChat.UserProfileService.Consumers.UserProfileApi.Contracts;

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
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7148")
        };
        httpClient.DefaultRequestHeaders.Add(UserProfileInternalApiClient.ApiKeyHeaderName, "internal-key");

        var client = new UserProfileInternalApiClient(httpClient);

        await client.CreateInitialUserProfileAsync(
            new CreateInitialUserProfileRequest
            {
                UserId = Guid.NewGuid(),
                FriendlyUserId = "jdoe",
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com"
            },
            CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://localhost:7148/internal/userprofiles/initial");
        handler.LastRequest.Headers.GetValues(UserProfileInternalApiClient.ApiKeyHeaderName).Single().Should().Be("internal-key");

        var payload = JsonSerializer.Deserialize<CreateInitialUserProfileRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        payload.Should().NotBeNull();
        payload!.FriendlyUserId.Should().Be("jdoe");
        payload.FirstName.Should().Be("John");
        payload.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task CreateInitialUserProfileAsync_WhenApiReturnsConflict_ThrowsNonTransientException()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent("conflict")
            }));
        var client = new UserProfileInternalApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7148")
        });

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.CreateInitialUserProfileAsync(new CreateInitialUserProfileRequest(), CancellationToken.None));

        exception.Message.Should().Contain("409");
    }

    [Fact]
    public async Task CreateInitialUserProfileAsync_WhenApiReturnsTaggedConcurrencyConflict_ThrowsHttpRequestException()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Conflict)
            {
                Content = new StringContent(
                    """{"detail":"conflict","error":"concurrency_conflict"}""",
                    Encoding.UTF8,
                    "application/problem+json")
            }));
        var client = new UserProfileInternalApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://localhost:7148")
        });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.CreateInitialUserProfileAsync(new CreateInitialUserProfileRequest(), CancellationToken.None));

        exception.Message.Should().Contain("409");
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
