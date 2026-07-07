using FlowChat.RealtimeService.Api.Realtime;
using FlowChat.RealtimeService.Api.Realtime.Notifications;
using FlowChat.RealtimeService.Application.Contracts.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace FlowChat.RealtimeService.UnitTests;

public sealed class SignalRRealtimeClientDispatcherTests
{
    [Fact]
    public async Task MessageReceivedAsync_SendsDeliveredAtUtcToSignalRClient()
    {
        var recipientUserId = Guid.NewGuid();
        var deliveredAtUtc = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        ChatMessageReceivedNotification? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.MessageReceived(It.IsAny<ChatMessageReceivedNotification>()))
            .Callback<ChatMessageReceivedNotification>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Groups(It.Is<IReadOnlyList<string>>(groups => groups.Contains(GroupNames.ForUser(recipientUserId)))))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.MessageReceivedAsync(
            new ChatMessageParam(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Jane",
                "Hello",
                42,
                new DateTimeOffset(2026, 5, 19, 11, 59, 0, TimeSpan.Zero),
                deliveredAtUtc,
                [recipientUserId]),
            CancellationToken.None);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.SequenceNum.Should().Be(42);
        capturedPayload!.DeliveredAtUtc.Should().Be(deliveredAtUtc);
    }

    [Fact]
    public async Task GroupConversationChangedAsync_SendsGroupConversationChangedToConversationGroup()
    {
        var participantUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        GroupConversationChangedNotification? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.GroupConversationChanged(It.IsAny<GroupConversationChangedNotification>()))
            .Callback<GroupConversationChangedNotification>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Group(GroupNames.ForConversation(conversationId)))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.GroupConversationChangedAsync(
            new GroupConversationChangedParam(
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

    [Fact]
    public async Task GroupConversationParticipantsAddedAsync_SendsNotificationToConversationGroup()
    {
        var participantUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        GroupConversationParticipantsAddedNotification? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.GroupConversationParticipantsAdded(It.IsAny<GroupConversationParticipantsAddedNotification>()))
            .Callback<GroupConversationParticipantsAddedNotification>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Group(GroupNames.ForConversation(conversationId)))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.GroupConversationParticipantsAddedAsync(
            new GroupConversationParticipantsAddedParam(conversationId, [participantUserId]),
            CancellationToken.None);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.ConversationId.Should().Be(conversationId);
        capturedPayload.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }

    [Fact]
    public async Task GroupConversationParticipantsRemovedAsync_SendsNotificationToConversationGroup()
    {
        var participantUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        GroupConversationParticipantsRemovedNotification? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.GroupConversationParticipantsRemoved(It.IsAny<GroupConversationParticipantsRemovedNotification>()))
            .Callback<GroupConversationParticipantsRemovedNotification>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Group(GroupNames.ForConversation(conversationId)))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.GroupConversationParticipantsRemovedAsync(
            new GroupConversationParticipantsRemovedParam(conversationId, [participantUserId]),
            CancellationToken.None);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.ConversationId.Should().Be(conversationId);
        capturedPayload.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }
}
