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

    [Fact]
    public async Task PublishConversationParticipantsAddedAsync_SendsChangedParticipantsAndTypeToRemoteInstance()
    {
        var conversationId = Guid.NewGuid();
        var participantUserId = Guid.NewGuid();
        var notification = new ConversationParticipantsAddedParam(
            conversationId,
            2,
            [participantUserId],
            [participantUserId, Guid.NewGuid()]);
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var client = new RealtimeInstanceInternalApiClient(new HttpClient(handler));

        await client.PublishConversationParticipantsAddedAsync(
            new Uri("http://instance-remote"),
            notification,
            CancellationToken.None);

        handler.LastRequest!.RequestUri!.ToString().Should().Be(
            "http://instance-remote/internal/realtime/conversations/participants-added/direct");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("conversationId").GetGuid().Should().Be(conversationId);
        document.RootElement.GetProperty("conversationType").GetInt32().Should().Be(2);
        document.RootElement.GetProperty("participantUserIds").EnumerateArray()
            .Select(element => element.GetGuid()).Should().Equal(participantUserId);
        document.RootElement.TryGetProperty("recipientUserIds", out _).Should().BeFalse();
    }

    [Fact]
    public async Task PublishConversationParticipantsRemovedAsync_SendsChangedParticipantsAndTypeToRemoteInstance()
    {
        var conversationId = Guid.NewGuid();
        var participantUserId = Guid.NewGuid();
        var notification = new ConversationParticipantsRemovedParam(
            conversationId,
            1,
            [participantUserId],
            [participantUserId]);
        var handler = new CapturingHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)));
        var client = new RealtimeInstanceInternalApiClient(new HttpClient(handler));

        await client.PublishConversationParticipantsRemovedAsync(
            new Uri("http://instance-remote"),
            notification,
            CancellationToken.None);

        handler.LastRequest!.RequestUri!.ToString().Should().Be(
            "http://instance-remote/internal/realtime/conversations/participants-removed/direct");
        using var document = JsonDocument.Parse(handler.LastRequestBody!);
        document.RootElement.GetProperty("conversationId").GetGuid().Should().Be(conversationId);
        document.RootElement.GetProperty("conversationType").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("participantUserIds").EnumerateArray()
            .Select(element => element.GetGuid()).Should().Equal(participantUserId);
    }
}
