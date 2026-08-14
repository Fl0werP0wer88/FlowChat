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
    public async Task MessageReceivedAsync_SendsDeliveredAtUtcToConversationGroup()
    {
        var conversationId = Guid.NewGuid();
        var deliveredAtUtc = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);
        ChatMessageReceivedNotification? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.MessageReceived(It.IsAny<ChatMessageReceivedNotification>()))
            .Callback<ChatMessageReceivedNotification>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Group(GroupNames.ForConversation(conversationId)))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.MessageReceivedAsync(
            new ChatMessageParam(
                Guid.NewGuid(),
                conversationId,
                Guid.NewGuid(),
                "Hello",
                42,
                new DateTimeOffset(2026, 5, 19, 11, 59, 0, TimeSpan.Zero),
                deliveredAtUtc,
                []),
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
    public async Task ConversationParticipantsAddedAsync_SendsNotificationToConversationGroup()
    {
        var participantUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        ConversationParticipantsAddedNotification? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.ConversationParticipantsAdded(It.IsAny<ConversationParticipantsAddedNotification>()))
            .Callback<ConversationParticipantsAddedNotification>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Group(GroupNames.ForConversation(conversationId)))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.ConversationParticipantsAddedAsync(
            new ConversationParticipantsAddedParam(conversationId, 2, [participantUserId], [participantUserId]),
            CancellationToken.None);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.ConversationId.Should().Be(conversationId);
        capturedPayload.ConversationType.Should().Be(2);
        capturedPayload.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }

    [Fact]
    public async Task ConversationParticipantsRemovedAsync_SendsNotificationToConversationGroup()
    {
        var participantUserId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        ConversationParticipantsRemovedNotification? capturedPayload = null;
        var realtimeClientMock = new Mock<IRealtimeClient>();
        realtimeClientMock
            .Setup(x => x.ConversationParticipantsRemoved(It.IsAny<ConversationParticipantsRemovedNotification>()))
            .Callback<ConversationParticipantsRemovedNotification>(payload => capturedPayload = payload)
            .Returns(Task.CompletedTask);
        var clientsMock = new Mock<IHubClients<IRealtimeClient>>();
        clientsMock
            .Setup(x => x.Group(GroupNames.ForConversation(conversationId)))
            .Returns(realtimeClientMock.Object);
        var hubContextMock = new Mock<IHubContext<ChatHub, IRealtimeClient>>();
        hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
        var dispatcher = new SignalRRealtimeClientDispatcher(hubContextMock.Object);

        await dispatcher.ConversationParticipantsRemovedAsync(
            new ConversationParticipantsRemovedParam(conversationId, 1, [participantUserId], [participantUserId]),
            CancellationToken.None);

        capturedPayload.Should().NotBeNull();
        capturedPayload!.ConversationId.Should().Be(conversationId);
        capturedPayload.ConversationType.Should().Be(1);
        capturedPayload.ParticipantUserIds.Should().ContainSingle().Which.Should().Be(participantUserId);
    }
}
