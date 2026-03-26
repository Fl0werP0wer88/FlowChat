using System.Net;
using System.Text.Json;
using FlowChat.Core.Exceptions;
using FlowChat.NotificationService.Consumers.Configuration;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using FlowChat.NotificationService.Consumers.Services;
using Microsoft.Extensions.Options;

namespace FlowChat.NotificationService.UnitTests;

public sealed class NotificationInternalApiClientTests
{
    [Fact]
    public async Task ProcessUserEmailVerificationRequestedAsync_PostsToExpectedEndpointWithApiKey()
    {
        string? requestBody = null;
        var handler = new CapturingHttpMessageHandler(async (request, _) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var client = new NotificationInternalApiClient(
            new HttpClient(handler),
            Options.Create(new NotificationApiSettings
            {
                BaseUrl = "https://localhost:7206",
                ApiKey = "internal-key"
            }));

        await client.ProcessUserEmailVerificationRequestedAsync(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = Guid.NewGuid(),
                Email = "john.doe@flowchat.local",
                UserName = "john.doe",
                DisplayName = "John Doe",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(
            "https://localhost:7206/internal/notifications/email-verification-requested",
            handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal(
            "internal-key",
            handler.LastRequest.Headers.GetValues(NotificationInternalApiClient.ApiKeyHeaderName).Single());

        var payload = JsonSerializer.Deserialize<ProcessUserEmailVerificationRequestedRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal("john.doe@flowchat.local", payload!.Email);
        Assert.Equal("john.doe", payload.UserName);
    }

    [Fact]
    public async Task ProcessUserEmailVerificationRequestedAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            }));
        var client = new NotificationInternalApiClient(
            new HttpClient(handler),
            Options.Create(new NotificationApiSettings
            {
                BaseUrl = "https://localhost:7206"
            }));

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.ProcessUserEmailVerificationRequestedAsync(
                new ProcessUserEmailVerificationRequestedRequest(),
                CancellationToken.None));

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
