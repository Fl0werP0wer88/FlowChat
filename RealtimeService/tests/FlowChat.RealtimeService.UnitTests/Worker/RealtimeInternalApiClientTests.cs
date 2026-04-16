using System.Net;
using System.Text.Json;
using AutoFixture;
using FlowChat.Core.Exceptions;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeInternalApiClientTests
{
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public async Task PublishMessageAsync_PostsToExpectedEndpointWithApiKey()
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

        var httpClient = new HttpClient(handlerMock.Object);
        httpClient.DefaultRequestHeaders.Add(RealtimeInternalApiClient.ApiKeyHeaderName, "internal-key");
        var client = new RealtimeInternalApiClient(httpClient);

        await client.PublishMessageAsync(
            new Uri("http://localhost:5215"),
            new PublishMessageRequest
            {
                MessageId = _fixture.Create<Guid>(),
                ConversationId = _fixture.Create<Guid>(),
                SenderUserId = _fixture.Create<Guid>(),
                SenderDisplayName = "John Doe",
                Text = "Hello",
                SentAtUtc = new DateTimeOffset(2026, 3, 17, 9, 0, 0, TimeSpan.Zero),
                RecipientUserIds = [_fixture.Create<Guid>()]
            },
            CancellationToken.None);

        sentRequest.Should().NotBeNull();
        sentRequest!.RequestUri!.ToString().Should().Be("http://localhost:5215/internal/realtime/messages");
        sentRequest.Headers.GetValues(RealtimeInternalApiClient.ApiKeyHeaderName).Single().Should().Be("internal-key");

        var payload = JsonSerializer.Deserialize<PublishMessageRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        payload.Should().NotBeNull();
        payload!.SenderDisplayName.Should().Be("John Doe");
    }

    [Fact]
    public async Task PublishMessageAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
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

        var client = new RealtimeInternalApiClient(new HttpClient(handlerMock.Object));

        var act = () => client.PublishMessageAsync(
            new Uri("http://localhost:5215"),
            new PublishMessageRequest(),
            CancellationToken.None);

        await act.Should().ThrowAsync<NonTransientException>()
            .WithMessage("*400*");
    }
}
