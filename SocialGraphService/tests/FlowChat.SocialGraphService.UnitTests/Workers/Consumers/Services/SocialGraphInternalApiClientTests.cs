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
    public async Task BulkUpsertOrDeleteUserProfileProjectionAsync_PostsToExpectedEndpointWithApiKey()
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

        await client.BulkUpsertOrDeleteUserProfileProjectionAsync(
            new BulkUpsertOrDeleteUserProfileProjectionRequest
            {
                Items =
                [
                    new BulkUpsertOrDeleteUserProfileProjectionRequestItem
                    {
                        UserProfileId = _fixture.Create<Guid>(),
                        SourceVersion = 7,
                        Value = new UserProfileProjectionRequest
                        {
                            FriendlyUserId = "jdoe",
                            MainEmailAddress = "jdoe@example.com",
                            MainEmailIsConfirmed = true,
                            MainEmailIsVisible = true,
                            Source = "user-profile-projection"
                        }
                    }
                ]
            },
            CancellationToken.None);

        sentRequest.Should().NotBeNull();
        sentRequest!.Method.Should().Be(HttpMethod.Post);
        sentRequest.RequestUri!.ToString().Should().Be("https://localhost:7194/internal/userprofiles/projection/bulk-upsert-or-delete");
        sentRequest.Headers.GetValues(SocialGraphInternalApiClient.ApiKeyHeaderName).Single().Should().Be("internal-key");

        var payload = JsonSerializer.Deserialize<BulkUpsertOrDeleteUserProfileProjectionRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        payload.Should().NotBeNull();
        var item = payload!.Items.Should().ContainSingle().Subject;
        item.SourceVersion.Should().Be(7);
        item.Value.Should().NotBeNull();
        item.Value!.FriendlyUserId.Should().Be("jdoe");
        item.Value.MainEmailAddress.Should().Be("jdoe@example.com");
        item.Value.MainEmailIsConfirmed.Should().BeTrue();
        item.Value.MainEmailIsVisible.Should().BeTrue();
    }

    [Fact]
    public async Task BulkUpsertOrDeleteUserProfileProjectionAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
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

        var act = () => client.BulkUpsertOrDeleteUserProfileProjectionAsync(new BulkUpsertOrDeleteUserProfileProjectionRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*400*");
    }
}
