using System.Net;
using System.Text.Json;
using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.NotificationService.Consumers.NotificationApi.Contracts;
using FlowChat.NotificationService.Consumers.Services;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace FlowChat.NotificationService.UnitTests;

public sealed class NotificationInternalApiClientTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task ProcessUserEmailVerificationRequestedAsync_PostsToExpectedEndpointWithApiKey()
    {
        string? requestBody = null;
        HttpRequestMessage? sentRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                sentRequest = request;
                requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Accepted));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:7206")
        };
        httpClient.DefaultRequestHeaders.Add(NotificationInternalApiClient.ApiKeyHeaderName, "internal-key");

        var client = new NotificationInternalApiClient(httpClient);

        await client.ProcessUserEmailVerificationRequestedAsync(
            new ProcessUserEmailVerificationRequestedRequest
            {
                UserId = _fixture.Create<Guid>(),
                Email = "john.doe@flowchat.local",
                UserName = "john.doe",
                DisplayName = "John Doe",
                ConfirmationLink = "https://localhost/confirm"
            },
            CancellationToken.None);

        sentRequest.Should().NotBeNull();
        sentRequest!.RequestUri!.ToString().Should().Be(
            "https://localhost:7206/internal/notifications/email-verification-requested");
        sentRequest.Headers.GetValues(NotificationInternalApiClient.ApiKeyHeaderName).Single().Should().Be("internal-key");

        var payload = JsonSerializer.Deserialize<ProcessUserEmailVerificationRequestedRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        payload.Should().NotBeNull();
        payload!.Email.Should().Be("john.doe@flowchat.local");
        payload.UserName.Should().Be("john.doe");
    }

    [Fact]
    public async Task ProcessUserEmailVerificationRequestedAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            });

        var client = new NotificationInternalApiClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:7206")
        });

        var act = () => client.ProcessUserEmailVerificationRequestedAsync(
            new ProcessUserEmailVerificationRequestedRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*400*");
    }
}
