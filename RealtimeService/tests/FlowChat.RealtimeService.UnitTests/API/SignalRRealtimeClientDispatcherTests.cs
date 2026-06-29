using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class SignalRRealtimeClientDispatcherTests
{
    [Fact]
    public async Task ReceiveMessageAsync_SendsDeliveredAtUtcToSignalRClient()
    {
        var recipientUserId = Guid.NewGuid();
        var deliveredAtUtc = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        ChatMessageNotificationDto? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.ReceiveMessage(It.IsAny<ChatMessageNotificationDto>()))
            .Callback<ChatMessageNotificationDto>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Groups(It.Is<IReadOnlyList<string>>(groups => groups.Contains(GroupNames.ForUser(recipientUserId)))))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.ReceiveMessageAsync(
            new ChatMessageParam(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Jane",
                "Hello",
                new DateTimeOffset(2026, 5, 19, 11, 59, 0, TimeSpan.Zero),
                deliveredAtUtc,
                [recipientUserId]),
            CancellationToken.None);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.DeliveredAtUtc.Should().Be(deliveredAtUtc);
    }

    [Fact]
    public async Task ConversationChangedAsync_SendsConversationChangedToParticipantGroups()
    {
        var participantUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        ConversationChangedDto? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.ConversationChanged(It.IsAny<ConversationChangedDto>()))
            .Callback<ConversationChangedDto>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Groups(It.Is<IReadOnlyList<string>>(groups => groups.Contains(GroupNames.ForUser(participantUserId)))))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.ConversationChangedAsync(
            new ConversationChangedParam(
                conversationId,
                2,
                "Dev Team",
                Guid.NewGuid(),
                [participantUserId]),
            CancellationToken.None);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.ConversationId.Should().Be(conversationId);
        capturedPayload.Type.Should().Be(2);
    }
}
