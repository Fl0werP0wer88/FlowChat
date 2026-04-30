using System.Net;
using System.Text;
using FlowChat.Core.Domain;
using FlowChat.RealtimeService.Application.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Presence;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class PresenceInternalApiClientTests
{
    [Fact]
    public async Task GetUserPresencePreferencesAsync_WhenPreferredStatusIsStringEnum_ReturnsStatus()
    {
        var userId = Guid.NewGuid();
        var client = CreateClient("""{"preferredStatus":"Busy"}""");

        var result = await client.GetUserPresencePreferencesAsync(userId, CancellationToken.None);

        result.Should().Be(PresenceStatus.Busy);
    }

    [Fact]
    public async Task GetContactPresenceStatusesAsync_WhenStatusIsStringEnum_ReturnsStatuses()
    {
        var userId = Guid.NewGuid();
        var contactUserId = Guid.NewGuid();
        var changedAtUtc = new DateTimeOffset(2026, 4, 30, 10, 15, 0, TimeSpan.Zero);
        var client = CreateClient(
            $$"""[{"userId":"{{contactUserId:D}}","status":"AFK","changedAtUtc":"{{changedAtUtc:O}}"}]""");

        var result = await client.GetContactPresenceStatusesAsync(userId, CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Should().Be(new ContactPresenceStatusDto(contactUserId, PresenceStatus.AFK, changedAtUtc));
    }

    private static PresenceInternalApiClient CreateClient(string responseBody)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object)
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
