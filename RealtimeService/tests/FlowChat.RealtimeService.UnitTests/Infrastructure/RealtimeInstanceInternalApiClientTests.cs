using System.Net;
using System.Text.Json;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FlowChat.RealtimeService.Infrastructure.Routing;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class RealtimeInstanceInternalApiClientTests
{
    [Fact]
    public async Task PublishMessageAsync_SendsDeliveredAtUtcToRemoteInstance()
    {
        var deliveredAtUtc = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        var notification = new ChatMessageParam(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Jane",
            "Hello",
            42,
            new DateTimeOffset(2026, 5, 19, 11, 59, 0, TimeSpan.Zero),
            deliveredAtUtc,
            [Guid.NewGuid()]);
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var client = new RealtimeInstanceInternalApiClient(new HttpClient(handler));

        await client.PublishMessageAsync(new Uri("http://instance-remote"), notification, CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be("http://instance-remote/internal/realtime/messages/direct");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("sequenceNum").GetInt64().Should().Be(42);
        document.RootElement.GetProperty("deliveredAtUtc").GetDateTimeOffset().Should().Be(deliveredAtUtc);
    }

    [Fact]
    public async Task PublishGroupConversationChangedAsync_SendsPayloadToRemoteInstance()
    {
        var conversationId = Guid.NewGuid();
        var notification = new GroupConversationChangedParam(
            conversationId,
            2,
            "Dev Team",
            Guid.NewGuid(),
            [Guid.NewGuid()]);
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var client = new RealtimeInstanceInternalApiClient(new HttpClient(handler));

        await client.PublishGroupConversationChangedAsync(new Uri("http://instance-remote"), notification, CancellationToken.None);

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().Be("http://instance-remote/internal/realtime/group-conversations/changed/direct");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("conversationId").GetGuid().Should().Be(conversationId);
    }
}
