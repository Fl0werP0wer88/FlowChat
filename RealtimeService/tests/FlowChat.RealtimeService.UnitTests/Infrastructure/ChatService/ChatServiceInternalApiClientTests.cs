using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FlowChat.RealtimeService.Infrastructure.ChatService;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class ChatServiceInternalApiClientTests
{
    [Fact]
    public async Task SetChatMessageSequenceNumberAsync_SendsSequenceNumberRequestAndReturnsSequenceNumber()
    {
        var messageId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = JsonContent.Create(new { SequenceNum = 42 })
        }));
        var client = CreateClient(handler);

        var result = await client.SetChatMessageSequenceNumberAsync(messageId, conversationId, CancellationToken.None);

        result.Should().Be(42);
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be($"/internal/messages/{messageId}/sequence-number");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("conversationId").GetGuid().Should().Be(conversationId);
    }

    [Fact]
    public async Task MarkChatMessageAsDeliveredAsync_SendsDeliveryRequest()
    {
        var messageId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var deliveredAtUtc = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var client = CreateClient(handler);

        await client.MarkChatMessageAsDeliveredAsync(messageId, conversationId, deliveredAtUtc, CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.PathAndQuery.Should().Be($"/internal/messages/{messageId}/delivery");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("conversationId").GetGuid().Should().Be(conversationId);
        document.RootElement.GetProperty("deliveredAtUtc").GetDateTimeOffset().Should().Be(deliveredAtUtc);
    }

    private static ChatServiceInternalApiClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5214")
        };

        return new ChatServiceInternalApiClient(httpClient);
    }
}
