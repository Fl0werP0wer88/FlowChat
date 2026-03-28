using System.Net;
using System.Text.Json;
using FlowChat.Core.Exceptions;
using FlowChat.RealtimeService.Consumers.Realtime.Contracts;
using FlowChat.RealtimeService.Consumers.Services;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeInternalApiClientTests
{
    [Fact]
    public async Task PublishMessageAsync_PostsToExpectedEndpointWithApiKey()
    {
        string? requestBody = null;
        var handler = new CapturingHttpMessageHandler(async (request, _) =>
        {
            requestBody = await request.Content!.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5215")
        };
        httpClient.DefaultRequestHeaders.Add(RealtimeInternalApiClient.ApiKeyHeaderName, "internal-key");
        var client = new RealtimeInternalApiClient(httpClient);

        await client.PublishMessageAsync(
            new PublishMessageRequest
            {
                MessageId = Guid.NewGuid(),
                ConversationId = Guid.NewGuid(),
                SenderUserId = Guid.NewGuid(),
                SenderDisplayName = "John Doe",
                Text = "Hello",
                SentAtUtc = new DateTime(2026, 3, 17, 9, 0, 0, DateTimeKind.Utc),
                RecipientUserIds = [Guid.NewGuid()]
            },
            CancellationToken.None);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal("http://localhost:5215/internal/realtime/messages", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("internal-key", handler.LastRequest.Headers.GetValues(RealtimeInternalApiClient.ApiKeyHeaderName).Single());

        var payload = JsonSerializer.Deserialize<PublishMessageRequest>(
            requestBody!,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(payload);
        Assert.Equal("John Doe", payload!.SenderDisplayName);
    }

    [Fact]
    public async Task PublishMessageAsync_WhenApiReturnsBadRequest_ThrowsNonTransientException()
    {
        var handler = new CapturingHttpMessageHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("bad request")
            }));
        var client = new RealtimeInternalApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5215")
        });

        var exception = await Assert.ThrowsAsync<NonTransientException>(() =>
            client.PublishMessageAsync(new PublishMessageRequest(), CancellationToken.None));

        Assert.Contains("400", exception.Message);
    }
}
