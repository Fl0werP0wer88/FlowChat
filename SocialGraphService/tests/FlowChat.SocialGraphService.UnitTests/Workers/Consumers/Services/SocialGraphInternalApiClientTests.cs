using System.Net;
using System.Text.Json;
using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.SocialGraphService.Consumers.Services;
using FlowChat.SocialGraphService.Consumers.SocialGraph.Contracts;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace FlowChat.SocialGraphService.UnitTests;

public sealed class SocialGraphInternalApiClientTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task UpsertUserProfileProjectionAsync_PostsToExpectedEndpointWithApiKey()
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
            BaseAddress = new Uri("https://localhost:7194")
        };
        httpClient.DefaultRequestHeaders.Add(SocialGraphInternalApiClient.ApiKeyHeaderName, "internal-key");

        var client = new SocialGraphInternalApiClient(httpClient);

        await client.UpsertUserProfileProjectionAsync(
            new UpsertUserProfileProjectionRequest
            {
                UserProfileId = _fixture.Create<Guid>(),
                UserName = "jdoe",
                DisplayName = "John Doe"
            },
            CancellationToken.None);

        sentRequest.Should().NotBeNull();
        sentRequest!.RequestUri!.ToString().Should().Be("https://localhost:7194/internal/userprofiles/projection");
        sentRequest.Headers.GetValues(SocialGraphInternalApiClient.ApiKeyHeaderName).Single().Should().Be("internal-key");

        var payload = JsonSerializer.Deserialize<UpsertUserProfileProjectionRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        payload.Should().NotBeNull();
        payload!.UserName.Should().Be("jdoe");
        payload.DisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task UpsertUserProfileProjectionAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
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

        var client = new SocialGraphInternalApiClient(new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://localhost:7194")
        });

        var act = () => client.UpsertUserProfileProjectionAsync(new UpsertUserProfileProjectionRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*400*");
    }
}
