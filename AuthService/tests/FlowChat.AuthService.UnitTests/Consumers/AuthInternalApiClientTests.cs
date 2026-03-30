using System.Net;
using FlowChat.AuthService.Consumers.AuthApi.Contracts;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Core.Exceptions;

namespace FlowChat.AuthService.UnitTests;

public sealed class AuthInternalApiClientTests
{
    [Fact]
    public async Task ConfirmEmailAsync_WhenApiReturnsNotFound_ThrowsTransientHttpRequestException()
    {
        var client = new AuthInternalApiClient(new HttpClient(new StubHttpMessageHandler(HttpStatusCode.NotFound))
        {
            BaseAddress = new Uri("https://localhost:7236")
        });

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.ConfirmEmailAsync(
                new AuthEmailConfirmationRequest
                {
                    EmailAddress = "john@example.com"
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmEmailAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
    {
        var client = new AuthInternalApiClient(new HttpClient(new StubHttpMessageHandler(HttpStatusCode.BadRequest))
        {
            BaseAddress = new Uri("https://localhost:7236")
        });

        await Assert.ThrowsAsync<NonTransientException>(() =>
            client.ConfirmEmailAsync(
                new AuthEmailConfirmationRequest
                {
                    EmailAddress = "john@example.com"
                },
                CancellationToken.None));
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("failure")
            });
        }
    }
}
