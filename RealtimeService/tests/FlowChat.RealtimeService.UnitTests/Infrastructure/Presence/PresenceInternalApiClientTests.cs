using System.Net;
using System.Text.Json;
using FlowChat.RealtimeService.Infrastructure.Presence;
using FluentAssertions;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PresenceInternalApiClientTests
{
    [Fact]
    public async Task InitializePresenceStatusAsync_SendsInitializeRequestWithUserId()
    {
        var userId = Guid.NewGuid();
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var client = CreateClient(handler);

        await client.InitializePresenceStatusAsync(userId, CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/internal/presence/status/initialize");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("userId").GetGuid().Should().Be(userId);
    }

    [Fact]
    public async Task DeletePresenceStatusAsync_SendsDeleteRequestWithUserId()
    {
        var userId = Guid.NewGuid();
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var client = CreateClient(handler);

        await client.DeletePresenceStatusAsync(userId, CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/internal/presence/status/delete");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("userId").GetGuid().Should().Be(userId);
    }

    [Fact]
    public async Task RefreshPresenceStatusAsync_SendsRefreshRequestWithUserIds()
    {
        var userId = Guid.NewGuid();
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = CreateClient(handler);

        await client.RefreshPresenceStatusAsync([userId], CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be("/internal/presence/status/refresh");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("userIds").EnumerateArray()
            .Should().ContainSingle()
            .Which.GetGuid().Should().Be(userId);
    }

    private static PresenceInternalApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5216")
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(x => x.CreateClient(PresenceInternalApiClient.HttpClientName))
            .Returns(httpClient);

        return new PresenceInternalApiClient(factoryMock.Object);
    }
}
